using Anytime_Anywhere_NAS.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Anytime_Anywhere_NAS.Services
{
	public class NasService
	{
		public SystemInfo GetSystemInfo()
		{
			Log.Information("Getting system information");
			try
			{
				var systemInfo = new SystemInfo
				{
					OS = GetOperatingSystem(),
					TotalCores = Environment.ProcessorCount,
					TotalRamGB = GetTotalMemoryGB(),
				};
				Log.Information("System info retrieved: OS={OS}, Cores={Cores}, RAM={RAM}GB", 
					systemInfo.OS, systemInfo.TotalCores, systemInfo.TotalRamGB);
				return systemInfo;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to retrieve system information");
				throw;
			}
		}

		public OSPlatform GetOperatingSystem()
		{
			OSPlatform platform;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				platform = OSPlatform.Windows;
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				platform = OSPlatform.Linux;
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				platform = OSPlatform.OSX;
			else
				platform = OSPlatform.FreeBSD;

			Log.Debug("Detected operating system: {Platform}", platform);
			return platform;
		}

		private double GetTotalMemoryGB()
		{
			Log.Debug("Attempting to retrieve total memory");
			try
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					try
					{
						using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
						{
							foreach (var obj in searcher.Get())
							{
								double totalKb = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
								double totalGB = Math.Round(totalKb / (1024 * 1024), 2);
								Log.Information("Retrieved Windows memory via WMI: {TotalGB}GB", totalGB);
								return totalGB;
							}
						}
					}
					catch (System.ComponentModel.Win32Exception ex)
					{
						Log.Warning(ex, "WMI access failed, unable to retrieve memory information");
						return 0;
					}
					catch (UnauthorizedAccessException ex)
					{
						Log.Warning(ex, "Unauthorized access to WMI, unable to retrieve memory information");
						return 0;
					}
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					var memInfo = File.ReadAllText("/proc/meminfo");
					var match = Regex.Match(memInfo, @"MemTotal:\s+(\d+) kB");
					if (match.Success)
					{
						double totalGB = Math.Round(double.Parse(match.Groups[1].Value) / (1024.0 * 1024.0), 2);
						Log.Information("Retrieved Linux memory from /proc/meminfo: {TotalGB}GB", totalGB);
						return totalGB;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error getting memory information");
			}
			Log.Warning("Unable to retrieve memory information, returning 0");
			return 0;
		}

		public async Task<ProcessResult> RunCommandAsync(string program, string args)
		{
			Log.Information("Running command: {Program} {Args}", program, args);
			
			var startInfo = new ProcessStartInfo()
			{
				FileName = program,
				Arguments = args,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			try
			{
				using var process = Process.Start(startInfo);
				if (process == null)
				{
					Log.Error("Process failed to start: {Program} {Args}", program, args);
					return new ProcessResult { ExitCode = -1, Error = "Process failed to start." };
				}

				var output = await process.StandardOutput.ReadToEndAsync();
				var error = await process.StandardError.ReadToEndAsync();

				await process.WaitForExitAsync();

				var result = new ProcessResult
				{
					Output = output,
					Error = error,
					ExitCode = process.ExitCode
				};

				if (result.IsSuccess)
				{
					Log.Information("Command completed successfully: {Program} {Args}", program, args);
					Log.Debug("Command output: {Output}", output);
				}
				else
				{
					Log.Warning("Command failed with exit code {ExitCode}: {Program} {Args}", 
						result.ExitCode, program, args);
					Log.Debug("Command error: {Error}", error);
				}

				return result;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Exception while running command: {Program} {Args}", program, args);
				return new ProcessResult 
				{ 
					ExitCode = -1, 
					Error = $"Exception: {ex.Message}" 
				};
			}
		}

		public async Task<bool> CheckForDockerAsync()
		{
			Log.Information("Checking for Docker installation");
			var result = await RunCommandAsync("docker", "--version");
			bool isInstalled = result.IsSuccess;
			
			if (isInstalled)
			{
				Log.Information("Docker is installed and running. Version: {Output}", result.Output.Trim());
			}
			else
			{
				Log.Warning("Docker is not installed or not running");
			}
			
			return isInstalled;
		}

		public async Task WriteComposeFileAsync(string storagePath, string shareName)
		{
			Log.Information("Writing docker-compose file. StoragePath={StoragePath}, ShareName={ShareName}", 
				storagePath, shareName);
			
			try
			{
				string fileContent = $@"
version: '3.8'
services:
  samba:
    image: dperson/samba
    container_name: my-simple-nas
    ports:
      - ""139:139""
      - ""445:445""
    volumes:
      - ""{storagePath}:/share""
    command: -s ""{shareName};/share;yes;no;no;all""
    restart: always
";
				await File.WriteAllTextAsync("docker-compose.yml", fileContent);
				Log.Information("docker-compose.yml file written successfully");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to write docker-compose.yml file");
				throw;
			}
		}

		public async Task<ProcessResult> StartNasAsync()
		{
			Log.Information("Starting NAS (docker-compose up)");
			var result = await RunCommandAsync("docker-compose", "up -d");
			
			if (result.IsSuccess)
			{
				Log.Information("NAS started successfully");
			}
			else
			{
				Log.Error("Failed to start NAS. ExitCode={ExitCode}, Error={Error}", 
					result.ExitCode, result.Error);
			}
			
			return result;
		}

		public async Task<ProcessResult> StopNasAsync()
		{
			Log.Information("Stopping NAS (docker-compose down)");
			var result = await RunCommandAsync("docker-compose", "down");
			
			if (result.IsSuccess)
			{
				Log.Information("NAS stopped successfully");
			}
			else
			{
				Log.Error("Failed to stop NAS. ExitCode={ExitCode}, Error={Error}", 
					result.ExitCode, result.Error);
			}
			
			return result;
		}
	}
}
