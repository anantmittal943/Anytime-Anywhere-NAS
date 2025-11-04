using Avalonia;
using ReactiveUI.Avalonia;
using Serilog;
using System;

namespace Anytime_Anywhere_NAS
{
	internal sealed class Program
	{
		// Initialization code. Don't use any Avalonia, third-party APIs or any
		// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
		// yet and stuff might break.
		[STAThread]
		public static void Main(string[] args)
		{
			// Configure Serilog
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Debug()
				.WriteTo.File("logs/anytime-nas-.log", 
					rollingInterval: RollingInterval.Day,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();

			try
			{
				Log.Information("Application starting up");
				BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Application terminated unexpectedly");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		// Avalonia configuration, don't remove; also used by visual designer.
		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.WithInterFont()
				.LogToTrace()
				.UseReactiveUI();
	}
}
