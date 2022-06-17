using System;
using System.Collections.Generic;
using System.Linq;
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

        public User this[int id] => HasClient(id) ? this.userFile.Data.First(x => x.Id == id) : null;

        public bool HasClient(int id) { return this.userFile.Data.Select(x => x.Id).Contains(id); }

        public User AddClient(User client)
        {
            if (!HasClient(client.Id))
            {
                this.userFile.Data.Add(client);
                this.userFile.Save();
            }
            return client;
        }

        public void Save()
        {
            userFile.Save();
        }



    }
}
