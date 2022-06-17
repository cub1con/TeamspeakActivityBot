using System.IO;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Manager
{
    public class ConfigManager
    {
        public Config Config => configFile.Data;
        private JsonFile<Config> configFile;

        public ConfigManager(string file)
        {
            configFile = new JsonFile<Config>(file);
        }

        public void Save()
        {
            configFile.Save();
        }

        /// <summary>
        /// Validates config
        /// </summary>
        /// <returns>returns true if config is valid</returns>
        public bool ValidateConfig()
        {
            LogHelper.LogUpdate("Validating config");

            bool error = false;
            if (this.Config.Host == "")
            {
                LogHelper.LogError("No Host set!");
                error = true;
            }

            if (this.Config.HostPort <= 0)
            {
                LogHelper.LogError("No valid Port set!");
                error = true;
            }

            if (this.Config.QueryUsername == "")
            {
                LogHelper.LogError("No QueryUsername set!");
                error = true;
            }

            if (this.Config.QueryPassword == "")
            {
                LogHelper.LogError("No QueryPassword set!");
                error = true;
            }

            // Only validate options if feature is enabled
            if (this.Config.TopListUpdateChannel)
            {
                if (this.Config.TopListChannelId <= 0)
                {
                    LogHelper.LogError("No valid TopListChannelId set!");
                    error = true;
                }

                if (!this.Config.TopListChannelNameFormat.Contains("%NAME%"))
                {
                    LogHelper.LogError("No Wildcard '%NAME%' in 'TopListChannelNameFormat found!");
                    error = true;
                }
            }

            if (this.Config.SentryDsn != "")
            {
                LogHelper.LogUpdate($"Using Sentry DSN: {this.Config.SentryDsn}");
            }

            if (error)
            {
                LogHelper.LogError("Config is not valid. Terminating.");
                return !error;
            }

            LogHelper.LogUpdate("Config validated");

            return !error;
        }
    }
}
