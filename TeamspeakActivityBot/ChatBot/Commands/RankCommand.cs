using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamspeakActivityBot.ChatBot.Commands.Abstraction;
using TeamspeakActivityBot.Extensions;
using TeamspeakActivityBot.Manager;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.ChatBot.Commands
{
    internal class RankCommand : IChatCommand
    {
        public string[] Name => new string[] { "rank" };

        public string HelpDescription => "Returns your current timerank (if time tracking is enabled)";

        public async Task<string> HandleCommand(TeamSpeakClient queryClient, int invokerId, TextCommand command)
        {
            // Get toplist ranking for user

            if (!ConfigManager.Config.TrackClientTimes)
            {
                return "Client time tracking not enabled.";
            }

            var clientId = await queryClient.GetUserByID(invokerId);

            var rankUser = UserManager.User(clientId.DatabaseId);

            return $"Your rank:\n"
                    + $"Active time: {UserManager.Users.OrderByDescending(x => x.ActiveTime).ToList().IndexOf(rankUser) + 1} - {rankUser.ActiveTime.GetAsDaysAndTime()}\n"
                    + $"Total time: {UserManager.Users.OrderByDescending(x => x.TotalTime).ToList().IndexOf(rankUser) + 1} - {rankUser.TotalTime.GetAsDaysAndTime()}";
        }
    }
}
