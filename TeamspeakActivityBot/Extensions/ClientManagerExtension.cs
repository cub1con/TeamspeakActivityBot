using System.Linq;
using TeamspeakActivityBot.Manager;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Extensions
{
    public static class ClientManagerExtension
    {
        public static User GetUserById(this UserManager clientManager, int clientId)
        {
            return clientManager.Clients.Data.FirstOrDefault(x => x.Id == clientId);
        }
    }
}
