using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace ReportManager
{
    /// <summary>
    /// The accessible config across the entire solution.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// The public Config accessor.
        /// </summary>
        public static Settings Settings = Settings.Read();
    }

    public class Settings
    {
        /// <summary>
        /// The webhook to send reports to.
        /// </summary>
        public string WebHook;

        public static Settings Read()
        {
            string configPath = Path.Combine(TShock.SavePath, "ReportConfig.json");
            if (!File.Exists(configPath))
            {
                TShock.Log.ConsoleError("No Report config found. Creating new one.");
                File.WriteAllText(configPath, JsonConvert.SerializeObject(Default(), Formatting.Indented));
                return Default();
            }
            try
            {
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(configPath));
            }
            catch { return Default(); }
        }

        private static Settings Default()
        {
            return new Settings()
            {
                WebHook = ""
            };
        }
    }
}
