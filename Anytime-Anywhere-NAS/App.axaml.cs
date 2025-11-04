using System.Linq;
using Anytime_Anywhere_NAS.ViewModels;
using Anytime_Anywhere_NAS.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Serilog;

namespace Anytime_Anywhere_NAS
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            Log.Information("Initializing Avalonia application");
            AvaloniaXamlLoader.Load(this);
            Log.Debug("Avalonia XAML loaded successfully");
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Log.Information("Framework initialization completed");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Log.Information("Running as desktop application");

                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();

                Log.Information("Creating main window");
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                Log.Information("Main window created successfully");
            }
            else
            {
                Log.Warning("Application is not running as desktop application");
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            Log.Debug("Disabling Avalonia data annotation validation");

            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }

            Log.Debug("Removed {Count} data validation plugins", dataValidationPluginsToRemove.Length);
        }
    }
}