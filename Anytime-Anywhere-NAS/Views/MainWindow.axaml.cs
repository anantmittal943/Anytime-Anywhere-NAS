using Anytime_Anywhere_NAS.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Serilog;
using System.Linq;

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

			var storageProvider = this.StorageProvider;
			var result = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = "Select a folder to share",
				AllowMultiple = false
			});

			if (result.Count > 0)
			{
				var folder = result[0];
				var path = folder.Path.LocalPath;
				
				if (!string.IsNullOrEmpty(path))
				{
					viewModel.SetSelectedFolder(path);
					Log.Information("User selected folder: {Path}", path);
				}
				else
				{
					Log.Warning("Could not get a file system path for the selected folder");
					viewModel.SetSelectedFolder("Error: Could not get folder path.");
				}
			}
			else
			{
				Log.Debug("User cancelled the folder picker");
			}
		}
	}
}