using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeamspeakActivityBot.Model;
using TeamspeakActivityBot.Utils;

namespace TeamspeakActivityBot.Manager
{
    public class ClientManager
    {
        public JsonFile<List<Client>> Clients { get; set; }

        public ClientManager(FileInfo file)
        {
            this.Clients = new JsonFile<List<Client>>(file);
        }

        public Client this[string clientId] => HasClient(clientId) ? this.Clients.Data.First(x => x.ClientId == clientId) : null;

        //public Client this[string clientId] => HasClient(clientId) ? clientFile.Data[clientId] : null;

        public bool HasClient(string clientId) { return this.Clients.Data.Select(x => x.ClientId).Contains(clientId); }
        public Client AddClient(Client client)
        {

            switch (HasClient(client.ClientId))
            {
                case true:
                    this.Clients.Data.First(x => x.ClientId.Equals(client.ClientId));
                    break;
                case false:
                    this.Clients.Data.Add(client);
                    break;
            }
            this.Clients.Save();
            return client;
        }




    }
}
