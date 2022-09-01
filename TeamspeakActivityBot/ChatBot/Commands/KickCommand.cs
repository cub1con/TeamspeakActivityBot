using System;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamspeakActivityBot.Chat.Commands.Abstraction;
using TeamspeakActivityBot.Extensions;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Chat.Commands
{
    public class KickCommand : IChatCommand
    {
        public string[] Name => new string[] { "kick" };

        public string HelpDescription => "[random/r, Optional Username] - kicks you, a random, or specified user";

        public async Task<string> HandleCommand(TeamSpeakClient queryClient, int invokerId, TextCommand command)
        {
            var userName = string.Empty;
            var userId = -1;

            // Check for argument
            if (command.Argument != null)
            {
                var users = await queryClient.GetFullClientsDetailedInfo();
                GetClientDetailedInfo user;

                // random picks a random user and kicks him
                if (command.Argument == "random" || command.Argument == "r")
                {
                    var rnd = new Random().Next(1, users.Count);
                    user = users[users.Count - 1];
                }
                else
                {
                    // Search for user to kick, report if not found
                    user = users.SingleOrDefault(x => x.NickName.ToLower().StartsWith(command.Argument));
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
