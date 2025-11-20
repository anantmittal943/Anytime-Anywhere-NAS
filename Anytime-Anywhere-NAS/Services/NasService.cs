using Anytime_Anywhere_NAS.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

		public async Task<string> DetectLinuxDistributionAsync()
		{
			Log.Information("Detecting Linux distribution");

			try
			{
				if (File.Exists("/etc/os-release"))
				{
					var osRelease = await File.ReadAllTextAsync("/etc/os-release");
					var idMatch = Regex.Match(osRelease, @"^ID=(.+)$", RegexOptions.Multiline);

					if (idMatch.Success)
					{
						string distro = idMatch.Groups[1].Value.Trim().Trim('"').ToLower();
						Log.Information("Detected Linux distribution: {Distro}", distro);
						return distro;
					}
				}

				if (File.Exists("/etc/debian_version"))
				{
					Log.Information("Detected Debian-based distribution");
					return "debian";
				}
				if (File.Exists("/etc/redhat-release"))
				{
					Log.Information("Detected Red Hat-based distribution");
					return "rhel";
				}
				if (File.Exists("/etc/arch-release"))
				{
					Log.Information("Detected Arch Linux");
					return "arch";
				}
				if (File.Exists("/etc/SuSE-release"))
				{
					Log.Information("Detected SUSE Linux");
					return "suse";
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error detecting Linux distribution");
			}

			Log.Warning("Unable to detect Linux distribution, defaulting to 'unknown'");
			return "unknown";
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

		public async Task<ProcessResult> CheckForDockerAsync()
		{
			Log.Information("Checking for Docker installation");
			var result = await RunCommandAsync("docker", "--version");

			if (result.IsSuccess)
			{
				Log.Information("Docker is installed. Version: {Output}", result.Output.Trim());
			}
			else
			{
				Log.Warning("Docker is not installed. Error: {Error}", result.Error);
			}

			return result;
		}

		public async Task<ProcessResult> CheckDockerRunningAsync()
		{
			Log.Information("Checking if Docker engine is running");
			var result = await RunCommandAsync("docker", "ps");

			if (result.IsSuccess)
			{
				Log.Information("Docker is running. Version: {Output}", result.Output.Trim());
			}
			else
			{
				Log.Warning("Docker is not running. Error: {Error}", result.Error);
			}

			return result;
		}

		public async Task WriteComposeFileAsync(string storagePath, string shareName, double cpuLimit, double memoryLimitGB)
		{
			Log.Information("Writing docker-compose file. StoragePath={StoragePath}, ShareName={ShareName}, CPU={CpuLimit}, RAM={MemoryLimitGB}",
				storagePath, shareName, cpuLimit, memoryLimitGB);

			try
			{
				string dockerPath = storagePath.Replace("\\", "/");

				Log.Debug("Normalized path for Docker: {DockerPath}", dockerPath);
				//command: -p -s ""{shareName};/share;yes;no;yes;nobody""
				//TODO: update the rules and access for the nas
				string fileContent = $@"
services:
  samba:
    image: dperson/samba
    container_name: my-simple-nas
    ports:
      - ""139:139""
      - ""445:445""
    volumes:
      - ""{dockerPath}:/share""
    command: -s ""{shareName};/share;yes;no;yes;all""
    restart: always
    cpus: ""{cpuLimit:0.0}""
    mem_limit: ""{memoryLimitGB:0.0}G""
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

			// First check if the container already exists
			var checkResult = await RunCommandAsync("docker", "ps -a --filter name=my-simple-nas --format {{.Names}}");
			
			if (checkResult.IsSuccess && checkResult.Output.Contains("my-simple-nas"))
			{
				Log.Information("Container 'my-simple-nas' already exists. Checking its state...");
				
				// Check if it's running
				var runningCheck = await RunCommandAsync("docker", "ps --filter name=my-simple-nas --format {{.Names}}");
				
				if (runningCheck.IsSuccess && runningCheck.Output.Contains("my-simple-nas"))
				{
					Log.Information("Container is already running. Restarting to apply new configuration...");
					await RunCommandAsync("docker", "stop my-simple-nas");
					await RunCommandAsync("docker", "rm my-simple-nas");
				}
				else
				{
					Log.Information("Container exists but is not running. Removing old container...");
					await RunCommandAsync("docker", "rm my-simple-nas");
				}
			}

			// Now start with docker-compose
			var result = await RunCommandAsync("docker", "compose up -d");

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
			var result = await RunCommandAsync("docker", "compose down");

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

		public async Task<ProcessResult> InstallDockerAsync()
		{
			Log.Information("Starting Docker installation process");

			var platform = GetOperatingSystem();

			if (platform == OSPlatform.Windows)
			{
				return await InstallDockerOnWindowsAsync();
			}
			else
			{
				Log.Error("Automatic Docker installation only supported on Windows");
				return new ProcessResult
				{
					ExitCode = -1,
					Error = "Automatic installation only available on Windows. Please install Docker manually."
				};
			}
		}

		private async Task<ProcessResult> InstallDockerOnWindowsAsync()
		{
			Log.Information("Installing Docker Desktop on Windows");

			Log.Information("Attempting installation via winget");
			var wingetResult = await RunCommandAsync("winget", "install -e --id Docker.DockerDesktop --accept-package-agreements --accept-source-agreements");

			if (wingetResult.IsSuccess)
			{
				Log.Information("Docker Desktop installed successfully via winget");
				return wingetResult;
			}

			Log.Warning("Winget installation failed, attempting chocolatey");

			var chocoResult = await RunCommandAsync("choco", "install docker-desktop -y");

			if (chocoResult.IsSuccess)
			{
				Log.Information("Docker Desktop installed successfully via chocolatey");
				return chocoResult;
			}

			Log.Error("Failed to install Docker Desktop automatically");
			return new ProcessResult
			{
				ExitCode = -1,
				Error = "Automatic installation failed. Please download Docker Desktop from https://www.docker.com/products/docker-desktop and install it manually."
			};
		}

		public void StartDockerDesktop()
		{
			Log.Information("Attempting to start Docker Desktop application...");

			string dockerExePath = @"C:\Program Files\Docker\Docker\Docker Desktop.exe";

			if (!File.Exists(dockerExePath))
			{
				Log.Error("Could not find Docker Desktop.exe at the standard path: {Path}", dockerExePath);
				throw new FileNotFoundException("Docker Desktop executable not found. Please ensure Docker Desktop is installed.");
			}

			try
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = dockerExePath,
					UseShellExecute = true,
					WindowStyle = ProcessWindowStyle.Hidden
				};

				Process.Start(startInfo);
				Log.Information("Docker Desktop application started successfully");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to start Docker Desktop application");
				throw;
			}
		}

		public string GetLocalIpAddress()
		{
			Log.Information("Detecting local IP address");
			try
			{
				// Get all network interfaces
				var interfaces = NetworkInterface.GetAllNetworkInterfaces();

				// logic to find the best match
				foreach (var network in interfaces)
				{
					// Skip adapters that are down, loopback (127.0.0.1), or virtual (Docker/WSL)
					if (network.OperationalStatus != OperationalStatus.Up || 
						network.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
						network.Description.ToLower().Contains("virtual") ||
						network.Description.ToLower().Contains("hyper-v") ||
						network.Description.ToLower().Contains("wsl") || 
						network.Name.ToLower().Contains("docker") || 
						network.Name.ToLower().Contains("vethernet"))
					{
						continue;
					}

					// We prioritize Ethernet and Wi-Fi
					if (network.NetworkInterfaceType == NetworkInterfaceType.Ethernet || 
						network.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
					{
						var properties = network.GetIPProperties();

						// We look for a gateway. Real networks usually have a gateway (router). 
						// Virtual ones often don't.
						if (properties.GatewayAddresses.Count == 0) continue;

						foreach (var address in properties.UnicastAddresses)
						{
							// We only want IPv4 (192.168.x.x), not IPv6
							if (address.Address.AddressFamily == AddressFamily.InterNetwork)
							{
								Log.Information("Found local IP: {IP} on interface {Name} ({Type})", 
									address.Address, network.Name, network.NetworkInterfaceType);
								
								return address.Address.ToString();
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error identifying local IP address");
			}

			Log.Warning("Could not detect local IP, falling back to localhost");
			return "127.0.0.1"; // Fallback to localhost if nothing found
		}
	}
}
