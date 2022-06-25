using System;
using System.Collections.Generic;
using System.Linq;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Manager
{
    public class UserManager
    {
        public List<User> Users => userFile.Data;
        private JsonFile<List<User>> userFile { get; set; }

        public UserManager(string file)
        {
            this.userFile = new JsonFile<List<User>>(file);
        }

        public User this[int id] => HasClient(id) ? this.Users.First(x => x.Id == id) : null;

        public bool HasClient(int id) { return this.Users.Select(x => x.Id).Contains(id); }

        private User AddUser(User client)
        {
            this.Users.Add(client);
            this.Save();
            return client;
        }

        public void Save()
        {
            userFile.Save();
        }

        public User GetUser(GetClientDetailedInfo clientInfo)
        {
            var client = this[clientInfo.DatabaseId];
            if (client == null)
            {
                return this.AddUser(new User()
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
                this.Save();
            }

            return client;
        }
    }
}
