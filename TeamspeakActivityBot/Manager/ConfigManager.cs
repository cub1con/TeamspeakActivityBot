﻿using System.IO;
using TeamspeakActivityBot.Model;
using TeamspeakActivityBot.Utils;

namespace TeamspeakActivityBot.Manager
{
    public class ConfigManager
    {
        public Config Config => configFile.Data;
        private JsonFile<Config> configFile;

        public ConfigManager(FileInfo file)
        {
            configFile = new JsonFile<Config>(file);
        }

        public void Save()
        {
            configFile.Save();
        }
    }
}