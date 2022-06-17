using System;
using System.Collections.Generic;
using System.Linq;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Manager
{
    public class UserManager
    {
        public JsonFile<List<User>> Clients { get; set; }

        public UserManager(string file)
        {
            this.Clients = new JsonFile<List<User>>(file);
        }

        public User this[int id] => HasClient(id) ? this.Clients.Data.First(x => x.Id == id) : null;

        public bool HasClient(int id) { return this.Clients.Data.Select(x => x.Id).Contains(id); }

        public User AddClient(User client)
        {
            if (!HasClient(client.Id))
            {
                this.Clients.Data.Add(client);
                this.Clients.Save();
            }
            return client;
        }




    }
}
