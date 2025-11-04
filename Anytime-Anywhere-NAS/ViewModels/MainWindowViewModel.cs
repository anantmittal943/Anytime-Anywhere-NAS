using Anytime_Anywhere_NAS.Services;
using ReactiveUI;
using Serilog;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Anytime_Anywhere_NAS.ViewModels
{
	public partial class MainWindowViewModel : ViewModelBase
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

		public ReactiveCommand<Unit, Unit> InstallDockerCommand { get; }
		public ReactiveCommand<Unit, Unit> StartNasCommand { get; }
		public ReactiveCommand<Unit, Unit> StopNasCommand { get; }

		public MainWindowViewModel()
		{
			Log.Information("Initializing MainWindowViewModel");
			_nasService = new NasService();

			// Detect OS platform
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
				var info = await Task.Run(() => _nasService.GetSystemInfo());
				Header = $"OS: {info.OS} | Cores: {info.TotalCores} | RAM: {info.TotalRamGB} GB";
				Log.Information("System information loaded successfully on startup");

				// Detect Linux distribution if on Linux
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
				IsDockerInstalled = await _nasService.CheckForDockerAsync();
				
				if (IsDockerInstalled)
				{
					NasStatus = "Docker is installed. Ready to start NAS.";
				}
				else
				{
					if (IsWindows)
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
				NasStatus = "Starting...";

				// Re-check Docker status before starting
				bool dockerAvailable = await _nasService.CheckForDockerAsync();
				if (!dockerAvailable)
				{
					IsDockerInstalled = false;
					
					if (IsWindows)
					{
						NasStatus = "Error: Docker is not running! Click 'Install Docker' to install it.";
					}
					else if (IsLinux)
					{
						NasStatus = "Error: Docker is not running! Please install and start Docker.";
					}
					else
					{
						NasStatus = "Error: Docker is not running!";
					}
					
					Log.Warning("Cannot start NAS: Docker is not available");
					return;
				}

				if (SelectedFolderPath == "No folder selected.")
				{
					NasStatus = "Error: Please select a folder to share first.";
					Log.Warning("Cannot start NAS: No folder selected");
					return;
				}

				Log.Information("Creating docker-compose configuration");
				await _nasService.WriteComposeFileAsync(SelectedFolderPath, "MyNasShare");

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
					NasStatus = $"Error: {result.Error}";
					Log.Error("NAS start failed with error: {Error}", result.Error);
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
