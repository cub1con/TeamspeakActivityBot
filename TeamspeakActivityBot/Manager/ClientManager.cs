using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Manager
{
    public class ClientManager
    {
        public JsonFile<List<Client>> Clients { get; set; }

        public ClientManager(FileInfo file)
        {
            this.Clients = new JsonFile<List<Client>>(file);
        }

        public Client this[int clientId] => HasClient(clientId) ? this.Clients.Data.First(x => x.ClientId == clientId) : null;

        //public Client this[string clientId] => HasClient(clientId) ? clientFile.Data[clientId] : null;

        public bool HasClient(int clientId) { return this.Clients.Data.Select(x => x.ClientId).Contains(clientId); }
        public Client AddClient(Client client)
        {
            if (!HasClient(client.ClientId))
            {
                this.Clients.Data.Add(client);
                this.Clients.Save();
            }
            return client;
        }




    }
}
