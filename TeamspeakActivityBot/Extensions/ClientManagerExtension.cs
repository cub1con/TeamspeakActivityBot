using System.Linq;
using TeamspeakActivityBot.Manager;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Extensions
{
    public static class ClientManagerExtension
    {
        public static Client GetUserById(this ClientManager clientManager, int clientId)
        {
            return clientManager.Clients.Data.FirstOrDefault(x => x.ClientId == clientId);
        }
    }
}
