using NLog;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Manager
{
    public class ConfigManager
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();

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
            Logger.Info("Validating config");

            bool error = false;
            if (this.Config.Host == "")
            {
                Logger.Error("No Host set!");
                error = true;
            }

            if (this.Config.HostPort <= 0)
            {
                Logger.Error("No valid Port set!");
                error = true;
            }

            if (this.Config.QueryUsername == "")
            {
                Logger.Error("No QueryUsername set!");
                error = true;
            }

            if (this.Config.QueryPassword == "")
            {
                Logger.Error("No QueryPassword set!");
                error = true;
            }

            // Only validate options if feature is enabled
            if (this.Config.TopListUpdateChannel)
            {
                if (this.Config.TopListChannelId <= 0)
                {
                    Logger.Error("No valid TopListChannelId set!");
                    error = true;
                }

                if (!this.Config.TopListChannelNameFormat.Contains("%NAME%"))
                {
                    Logger.Error("No Wildcard '%NAME%' in 'TopListChannelNameFormat found!");
                    error = true;
                }
            }

            if (this.Config.SentryDsn != "")
            {
                Logger.Info($"Using Sentry");
            }

            if (error)
            {
                Logger.Error("Config is not valid. Terminating.");
                return !error;
            }

            Logger.Info("Config validated");

            return !error;
        }
    }
}
