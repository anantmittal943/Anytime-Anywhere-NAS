using Anytime_Anywhere_NAS.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Anytime_Anywhere_NAS.Services
{
	public class SettingsService
	{
		private static string GetConfigPath()
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var configDir = Path.Combine(appData, "AnytimeAnywhereNAS");

			if (!Directory.Exists(configDir))
			{
				Directory.CreateDirectory(configDir);
			}

			return Path.Combine(configDir, "settings.json");
		}

		public static void Save(AppSettings appSettings)
		{
			try
			{
				var json = JsonSerializer.Serialize(appSettings);
				File.WriteAllText(GetConfigPath(), json);
				Log.Debug("Settings saved to {Path}", GetConfigPath());
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to save settings");
			}
		}

		public static AppSettings Load()
		{
			try
			{
				var path = GetConfigPath();
				if (File.Exists(path))
				{
					var json = File.ReadAllText(path);
					var appSettings = JsonSerializer.Deserialize<AppSettings>(json);
					Log.Debug("Settings loaded from {Path}", path);
					return appSettings ?? new AppSettings();
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to load settings");
			}
			return new AppSettings();
		}
	}
}
