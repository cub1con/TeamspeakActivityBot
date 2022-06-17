using System.Linq;
using TeamspeakActivityBot.Manager;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Extensions
{
    public static class UserManagerExtension
    {
        public static User GetUserById(this UserManager clientManager, int clientId)
        {
            return clientManager.Users.FirstOrDefault(x => x.Id == clientId);
        }
    }
}
