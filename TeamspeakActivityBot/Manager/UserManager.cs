using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Manager
{
    public static class UserManager
    {
        private static string CLIENTS_FILE = Path.Combine(Environment.CurrentDirectory, "clients.json");
        private static Logger Logger = LogManager.GetCurrentClassLogger();

        public static List<User> Users => userFile.Data;
        private static JsonFile<List<User>> userFile { get; set; }

        private static JsonFile<List<User>> LoadUserfile()
        {
            Logger.Trace($"Initial loading of {CLIENTS_FILE}");
            return new JsonFile<List<User>>(CLIENTS_FILE, false);
        }

        public static User User(int id)
        {
            return HasClient(id) ? Users.First(x => x.Id == id) : null;
        }

        public static bool HasClient(int id) { return Users.Select(x => x.Id).Contains(id); }

        private static User AddUser(User client)
        {
            Users.Add(client);
            return client;
        }

        public static void Save()
        {
            Logger.Trace("Saving");
            userFile.Save();
        }

        public static User GetUser(GetClientDetailedInfo clientInfo)
        {
            if (clientInfo == null)
                return null;

            var client = User(clientInfo.DatabaseId);
            if (client == null)
            {
                return AddUser(new User()
                {
                    Id = clientInfo.DatabaseId,
                    DisplayName = clientInfo.NickName,
                    ActiveTime = TimeSpan.Zero,
                    TotalTime = TimeSpan.Zero
                });
            }


            // If the user changed the nickname, update the DisplayName
            if (client.DisplayName != clientInfo.NickName)
            {
                client.DisplayName = clientInfo.NickName;
            }

            return client;
        }

        public static void Load()
        {
            Logger.Trace("Loading");
            if (userFile == null)
                userFile = LoadUserfile();


            userFile.Read();
        }
    }
}
