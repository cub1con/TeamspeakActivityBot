using System;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamspeakActivityBot.ChatBot.Commands.Abstraction;
using TeamspeakActivityBot.Extensions;

namespace TeamspeakActivityBot.ChatBot.Commands
{
    public class KickCommand : ChatCommand
    {
        public string[] Name => new string[] { "kick" };

        public string HelpDescription => "[random/r, Optional Username] - kicks you, a random, or specified user";

        public async Task<string> HandleCommand(TeamSpeakClient queryClient, int invokerId, string arguments)
        {
            var userName = string.Empty;
            var userId = -1;

            // Check for argument
            if (arguments != null)
            {
                var users = await queryClient.GetFullClientsDetailedInfo();
                GetClientDetailedInfo user;

                // random picks a random user and kicks him
                if (arguments == "random" || arguments == "r")
                {
                    var rnd = new Random().Next(1, users.Count);
                    user = users[users.Count - 1];
                }
                else
                {
                    // Search for user to kick, report if not found
                    user = users.SingleOrDefault(x => x.NickName.ToLower().StartsWith(arguments));
                    if (user == null)
                    {
                        return "User not found";
                    }
                }

                userName = user.NickName;
                userId = user.DatabaseId;
            }
            else
            {
                userId = invokerId;
                userName = "You";
            }

            // Kick the user
            await queryClient.KickClient(userId, KickOrigin.Server, "https://kek.com");
            return $"{userName} got kicked from the server!";
        }
    }
}
