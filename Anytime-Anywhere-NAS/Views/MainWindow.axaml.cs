using Anytime_Anywhere_NAS.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Anytime_Anywhere_NAS.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Log.Information("MainWindow initialized");
        }

		private async void OnSelectFolderClick(object sender, RoutedEventArgs e)
		{
			Log.Debug("Folder picker button clicked");
			
			var viewModel = this.DataContext as MainWindowViewModel;
			if (viewModel == null)
			{
				Log.Warning("DataContext is not MainWindowViewModel");
				return;
			}

			try
			{
				var storageProvider = this.StorageProvider;
				
				IStorageFolder? suggestedStartLocation = null;
				try
				{
					suggestedStartLocation = await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads);
					Log.Debug("Using Downloads folder as suggested start location");
				}
				catch (Exception ex)
				{
					Log.Warning(ex, "Could not get Downloads folder, using default location");
				}

				var options = new FolderPickerOpenOptions
				{
					Title = "Select a folder to share",
					AllowMultiple = false,
					SuggestedStartLocation = suggestedStartLocation
				};

				var result = await storageProvider.OpenFolderPickerAsync(options);

				if (result.Count > 0)
				{
					var folder = result[0];
					
					if (folder.TryGetLocalPath() is { } localPath)
					{
						viewModel.SetSelectedFolder(localPath);
						Log.Information("User selected folder: {Path}", localPath);
					}
					else
					{
						var path = folder.Path.LocalPath;
						if (!string.IsNullOrEmpty(path))
						{
							viewModel.SetSelectedFolder(path);
							Log.Information("User selected folder (via Path.LocalPath): {Path}", path);
						}
						else
						{
							Log.Warning("Could not get a local file system path for the selected folder");
							viewModel.SetSelectedFolder("Error: Could not get folder path.");
						}
					}
				}
				else
				{
					Log.Debug("User cancelled the folder picker");
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error occurred while opening folder picker");
				viewModel?.SetSelectedFolder($"Error: {ex.Message}");
			}
		}
	}
}