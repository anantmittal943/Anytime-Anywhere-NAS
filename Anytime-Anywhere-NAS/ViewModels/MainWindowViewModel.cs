using Anytime_Anywhere_NAS.Models;
using Anytime_Anywhere_NAS.Services;
using ReactiveUI;
using Serilog;
using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Anytime_Anywhere_NAS.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private string _header = "Loading system information...";
		public string Header
		{
			get => _header;
			set => this.RaiseAndSetIfChanged(ref _header, value);
		}

		private string _selectedFolderPath = "No folder selected.";
		public string SelectedFolderPath
		{
			get => _selectedFolderPath;
			set
			{
				Log.Information("Selected folder path changed to: {Path}", value);
				this.RaiseAndSetIfChanged(ref _selectedFolderPath, value);
			}
		}

		private string _nasStatus = "NAS is Stopped.";
		public string NasStatus
		{
			get => _nasStatus;
			set => this.RaiseAndSetIfChanged(ref _nasStatus, value);
		}

		private bool _isNasRunning = false;
		public bool IsNasRunning
		{
			get => _isNasRunning;
			set => this.RaiseAndSetIfChanged(ref _isNasRunning, value);
		}

		private bool _isDockerInstalled = false;
		public bool IsDockerInstalled
		{
			get => _isDockerInstalled;
			set => this.RaiseAndSetIfChanged(ref _isDockerInstalled, value);
		}

		private bool _isLinux = false;
		public bool IsLinux
		{
			get => _isLinux;
			set => this.RaiseAndSetIfChanged(ref _isLinux, value);
		}

		private bool _isWindows = false;
		public bool IsWindows
		{
			get => _isWindows;
			set => this.RaiseAndSetIfChanged(ref _isWindows, value);
		}

		private string _linuxDistro = "";
		public string LinuxDistro
		{
			get => _linuxDistro;
			set => this.RaiseAndSetIfChanged(ref _linuxDistro, value);
		}

		private NasService _nasService;

		private SystemInfo _systemInfo;

		public ReactiveCommand<Unit, Unit> InstallDockerCommand { get; }
		public ReactiveCommand<Unit, Unit> StartNasCommand { get; }
		public ReactiveCommand<Unit, Unit> StopNasCommand { get; }

		public MainWindowViewModel()
		{
			Log.Information("Initializing MainWindowViewModel");
			_nasService = new NasService();
			_systemInfo = new SystemInfo();

			var platform = _nasService.GetOperatingSystem();
			IsLinux = platform == OSPlatform.Linux;
			IsWindows = platform == OSPlatform.Windows;

			var canInstallDocker = this.WhenAnyValue(
				x => x.IsDockerInstalled, 
				x => x.IsWindows,
				(installed, windows) => !installed && windows);
			
			var canStart = this.WhenAnyValue(
				x => x.IsNasRunning, 
				x => x.IsDockerInstalled,
				(isRunning, dockerInstalled) => !isRunning && dockerInstalled);
			
			var canStop = this.WhenAnyValue(x => x.IsNasRunning);

			InstallDockerCommand = ReactiveCommand.CreateFromTask(InstallDockerAsync, canInstallDocker);
			StartNasCommand = ReactiveCommand.CreateFromTask(StartNasAsync, canStart);
			StopNasCommand = ReactiveCommand.CreateFromTask(StopNasAsync, canStop);

			_ = LoadSystemInfoAsync();
			_ = CheckDockerStatusAsync();

			Log.Information("MainWindowViewModel initialized successfully");
		}

		private async Task LoadSystemInfoAsync()
		{
			Log.Information("Loading system information on startup");
			try
			{
				_systemInfo = await Task.Run(() => _nasService.GetSystemInfo());
				Header = $"OS: {_systemInfo.OS} | Cores: {_systemInfo.TotalCores} | RAM: {_systemInfo.TotalRamGB} GB";
				Log.Information("System information loaded successfully on startup");

				if (IsLinux)
				{
					LinuxDistro = await _nasService.DetectLinuxDistributionAsync();
					Log.Information("Detected Linux distribution: {Distro}", LinuxDistro);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to load system information on startup");
				Header = $"Error loading system info: {ex.Message}";
			}
		}

		private async Task CheckDockerStatusAsync()
		{
			Log.Information("Checking Docker status on startup");
			try
			{
				var dockerCheckResult = await _nasService.CheckForDockerAsync();
				IsDockerInstalled = dockerCheckResult.IsSuccess;
				
				if (IsDockerInstalled)
				{
					NasStatus = "Docker is installed. Ready to start NAS.";
				}
				else
				{
					if (IsLinux && dockerCheckResult.Error.Contains("permission denied", StringComparison.OrdinalIgnoreCase))
					{
						NasStatus = "Docker permission denied. Run: sudo usermod -aG docker $USER (then logout/login)";
						Log.Error("--- DOCKER PERMISSION ERROR DETECTED ---");
						Log.Error("Docker is installed but you don't have permission to use it.");
						Log.Error("HOW TO FIX:");
						Log.Error("1. Open a terminal and run: sudo usermod -aG docker $USER");
						Log.Error("2. Log out of your Linux session and log back in (or reboot).");
						Log.Error("3. Restart this application.");
						Log.Error("----------------------------------------");
					}
					else if (IsWindows)
					{
						NasStatus = "Docker not detected. Click 'Install Docker' to proceed.";
					}
					else if (IsLinux)
					{
						NasStatus = "Docker not detected. Please install Docker manually.";
					}
					else
					{
						NasStatus = "Docker not detected. Please install Docker for your operating system.";
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error checking Docker status");
				NasStatus = $"Error checking Docker: {ex.Message}";
			}
		}

		private async Task InstallDockerAsync()
		{
			Log.Information("User initiated Docker installation");
			try
			{
				NasStatus = "Installing Docker... This may take several minutes.";
				
				var result = await _nasService.InstallDockerAsync();
				
				if (result.IsSuccess)
				{
					IsDockerInstalled = true;
					NasStatus = "Docker installed successfully! You may need to restart your system.";
					Log.Information("Docker installation completed successfully");
				}
				else
				{
					NasStatus = $"Docker installation failed: {result.Error}";
					Log.Error("Docker installation failed: {Error}", result.Error);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Unexpected error during Docker installation");
				NasStatus = $"Error installing Docker: {ex.Message}";
			}
		}

		private async Task StartNasAsync()
		{
			Log.Information("User initiated NAS start");
			try
			{
				NasStatus = "Starting... (Checking for Docker)";

				var dockerCheckResult = await _nasService.CheckDockerRunningAsync();
				bool dockerAvailable = dockerCheckResult.IsSuccess;
				
				if (!dockerAvailable)
				{
					if (IsLinux && dockerCheckResult.Error.Contains("permission denied", StringComparison.OrdinalIgnoreCase))
					{
						Log.Error("--- DOCKER PERMISSION ERROR DETECTED ---");
						Log.Error("Docker is installed but you don't have permission to use it.");
						Log.Error("HOW TO FIX:");
						Log.Error("1. Open a terminal and run: sudo usermod -aG docker $USER");
						Log.Error("2. Log out of your Linux session completely and log back in (or reboot).");
						Log.Error("3. Restart this application.");
						Log.Error("The group changes only take effect after you log back in!");
						Log.Error("----------------------------------------");
						
						NasStatus = "Error: Docker permission denied. Check logs for fix instructions.";
						IsDockerInstalled = false;
						return;
					}
					
					if (IsWindows)
					{
						Log.Warning("Docker engine is not running. Attempting to start Docker Desktop...");
						NasStatus = "Docker not running... Starting Docker Desktop. Please wait 30 seconds.";
						
						try
						{
							_nasService.StartDockerDesktop();
							
							await Task.Delay(30000);
							
							dockerCheckResult = await _nasService.CheckDockerRunningAsync();
							dockerAvailable = dockerCheckResult.IsSuccess;
							
							if (!dockerAvailable)
							{
								NasStatus = "Error: Docker Desktop started but failed to connect. Please check Docker Desktop.";
								Log.Error("Failed to connect to Docker after starting Docker Desktop");
								return;
							}
							
							Log.Information("Docker Desktop started successfully and engine is now running");
							NasStatus = "Docker started successfully. Continuing...";
						}
						catch (FileNotFoundException)
						{
							NasStatus = "Error: Docker Desktop is not installed. Please install it first.";
							Log.Error("Docker Desktop executable not found");
							IsDockerInstalled = false;
							return;
						}
						catch (Exception ex)
						{
							NasStatus = $"Error: Failed to start Docker Desktop: {ex.Message}";
							Log.Error(ex, "Failed to start Docker Desktop");
							return;
						}
					}
					else if (IsLinux)
					{
						NasStatus = "Error: Docker is not running! Please start Docker service: sudo systemctl start docker";
						Log.Warning("Cannot start NAS: Docker service is not running on Linux");
						IsDockerInstalled = false;
						return;
					}
					else
					{
						NasStatus = "Error: Docker is not installed or not running!";
						Log.Warning("Cannot start NAS: Docker is not available");
						IsDockerInstalled = false;
						return;
					}
				}

				Log.Information("Docker is running. Proceeding with NAS start.");
				NasStatus = "Docker OK. Starting NAS...";

				if (SelectedFolderPath == "No folder selected.")
				{
					NasStatus = "Error: Please select a folder to share first.";
					Log.Warning("Cannot start NAS: No folder selected");
					return;
				}

				Log.Information("Calculating resource limits...");
				double cpuLimit = Math.Min(2.0, Math.Max(0.5, _systemInfo.TotalCores * 0.25));
				double memoryLimitGB = Math.Min(3.0, Math.Max(1.0, _systemInfo.TotalRamGB * 0.25));

				Log.Information("Allocating {CpuLimit:0.0} CPUs and {MemoryLimitGB:0.0}GB RAM", cpuLimit, memoryLimitGB);

				Log.Information("Creating docker-compose configuration");
				await _nasService.WriteComposeFileAsync(SelectedFolderPath, "MyNasShare", cpuLimit, memoryLimitGB);

				Log.Information("Starting Docker containers");
				var result = await _nasService.StartNasAsync();

				if (result.IsSuccess)
				{
					IsNasRunning = true;
					NasStatus = "NAS is RUNNING.";
					Log.Information("NAS started successfully");
				}
				else
				{
					if (IsLinux && result.Error.Contains("permission denied", StringComparison.OrdinalIgnoreCase))
					{
						Log.Error("--- DOCKER PERMISSION ERROR ---");
						Log.Error("Run: sudo usermod -aG docker $USER");
						Log.Error("Then logout and login again.");
						Log.Error("-------------------------------");
						NasStatus = "Error: Docker permission denied. See logs.";
					}
					else
					{
						NasStatus = $"Error: {result.Error}";
						Log.Error("NAS start failed with error: {Error}", result.Error);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Unexpected error while starting NAS");
				NasStatus = $"Error: {ex.Message}";
			}
		}

		private async Task StopNasAsync()
		{
			Log.Information("User initiated NAS stop");
			try
			{
				NasStatus = "Stopping...";
				var result = await _nasService.StopNasAsync();

				if (result.IsSuccess)
				{
					IsNasRunning = false;
					NasStatus = "NAS is Stopped.";
					Log.Information("NAS stopped successfully");
				}
				else
				{
					NasStatus = $"Error: {result.Error}";
					Log.Error("NAS stop failed with error: {Error}", result.Error);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Unexpected error while stopping NAS");
				NasStatus = $"Error: {ex.Message}";
			}
		}

		public void SetSelectedFolder(string path)
		{
			Log.Information("Setting selected folder: {Path}", path);
			SelectedFolderPath = path;
		}
	}
}
