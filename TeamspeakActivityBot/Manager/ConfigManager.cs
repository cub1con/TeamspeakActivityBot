using NLog;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Manager
{
    public static class ConfigManager
    {
#if DEBUG
        private static string CONFIG_FILE = Path.Combine(Environment.CurrentDirectory, "config-dev.json");
#else
        private static string CONFIG_FILE = Path.Combine(Environment.CurrentDirectory, "config.json");
#endif

        private static Logger Logger = LogManager.GetCurrentClassLogger();

        public static Config Config => configFile.Data;

        private static JsonFile<Config> configFile;

        private static JsonFile<Config> LoadConfig()
        {
            Logger.Trace($"Initial loading of {CONFIG_FILE}");
            return new JsonFile<Config>(CONFIG_FILE);
        }


        public static void Save()
        {
            Logger.Trace("Saving");
            configFile.Save();
        }

        /// <summary>
        /// Validates config
        /// </summary>
        /// <returns>returns true if config is valid</returns>
        public static bool ValidateConfig()
        {
            Logger.Info("Validating config");

            bool error = false;
            if (Config.Host == "")
            {
                Logger.Error("No Host set!");
                error = true;
            }

            if (Config.HostPort <= 0)
            {
                Logger.Error("No valid Port set!");
                error = true;
            }

            if (Config.QueryUsername == "")
            {
                Logger.Error("No QueryUsername set!");
                error = true;
            }

            if (Config.QueryPassword == "")
            {
                Logger.Error("No QueryPassword set!");
                error = true;
            }

            if (string.IsNullOrWhiteSpace(Config.BotName))
            {
                Logger.Error("No BotName set!");
                error = true;
            }

            // Only validate options if feature is enabled
            if (Config.TopListUpdateChannel)
            {
                if (Config.TopListChannelId <= 0)
                {
                    Logger.Error("No valid TopListChannelId set!");
                    error = true;
                }

                if (!Config.TopListChannelNameFormat.Contains("%NAME%"))
                {
                    Logger.Error("No Wildcard '%NAME%' in 'TopListChannelNameFormat found!");
                    error = true;
                }
            }

            if (Config.SentryDsn != "")
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

        public static void Load()
        {
            Logger.Trace("Loading");
            if (configFile == null)
                configFile = LoadConfig();


            configFile.Read();
        }
    }
}
