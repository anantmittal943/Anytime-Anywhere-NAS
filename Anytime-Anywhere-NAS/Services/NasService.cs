using Anytime_Anywhere_NAS.Models;
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
			return new SystemInfo
			{
				OS = GetOperatingSystem(),
				TotalCores = Environment.ProcessorCount,
				TotalRamGB = GetTotalMemoryGB(),
			};
		}

		public OSPlatform GetOperatingSystem()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return OSPlatform.Windows;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				return OSPlatform.Linux;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				return OSPlatform.OSX;
			return OSPlatform.FreeBSD;
		}

		private double GetTotalMemoryGB()
		{
			try
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
					{
						foreach (var obj in searcher.Get())
						{
							double totalKb = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
							return Math.Round(totalKb / (1024 * 1024), 2);
						}
					}
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					var memInfo = File.ReadAllText("/proc/meminfo");
					var match = Regex.Match(memInfo, @"MemTotal:\s+(\d+) kB");
					if (match.Success)
					{
						return Math.Round(double.Parse(match.Groups[1].Value) / (1024.0 * 1024.0), 2);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error getting memory: {e.Message}");
			}
			return 0;
		}

		public async Task<ProcessResult> RunCommandAsync(string program, string args)
		{
			var startInfo = new ProcessStartInfo()
			{
				FileName = program,
				Arguments = args,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = true,
				CreateNoWindow = true,
			};

			using var process = Process.Start(startInfo);
			if (process == null)
			{
				return new ProcessResult { ExitCode = -1, Error = "Process failed to start." };
			}

			var output = await process.StandardOutput.ReadToEndAsync();
			var error = await process.StandardError.ReadToEndAsync();

			await process.WaitForExitAsync();
			
			return new ProcessResult
			{
				Output = output,
				Error = error,
				ExitCode = process.ExitCode
			};
		}
	}
}
