using Anytime_Anywhere_NAS.Services;
using ReactiveUI;
using Serilog;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace Anytime_Anywhere_NAS.ViewModels
{
	public partial class MainWindowViewModel : ViewModelBase
	{
		private string _header = "Click 'Scan' to check system specs.";
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

		private NasService _nasService;

		public ReactiveCommand<Unit, Unit> ScanSystemCommand { get; }
		public ReactiveCommand<Unit, Unit> StartNasCommand { get; }
		public ReactiveCommand<Unit, Unit> StopNasCommand { get; }

		public MainWindowViewModel()
		{
			Log.Information("Initializing MainWindowViewModel");
			_nasService = new NasService();

			ScanSystemCommand = ReactiveCommand.CreateFromTask(ScanSystemAsync);
			StartNasCommand = ReactiveCommand.CreateFromTask(StartNasAsync);
			StopNasCommand = ReactiveCommand.CreateFromTask(StopNasAsync);
			
			Log.Information("MainWindowViewModel initialized successfully");
		}

		private async Task ScanSystemAsync()
		{
			Log.Information("User initiated system scan");
			try
			{
				Header = "Scanning...";
				var info = await Task.Run(() => _nasService.GetSystemInfo());
				Header = $"OS: {info.OS} | Cores: {info.TotalCores} | RAM: {info.TotalRamGB} GB";
				Log.Information("System scan completed successfully");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "System scan failed");
				Header = $"Error: {ex.Message}";
			}
		}

		private async Task StartNasAsync()
		{
			Log.Information("User initiated NAS start");
			try
			{
				NasStatus = "Starting...";

				if (!await _nasService.CheckForDockerAsync())
				{
					NasStatus = "Error: Docker is not installed or not running!";
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
		 	
