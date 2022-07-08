using System;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamspeakActivityBot.ChatBot.Commands.Abstraction;

namespace TeamspeakActivityBot.ChatBot.Commands
{
    public class HelpCommand : ChatCommand
    {
        public string[] Name => new string[] { "help" };

        public string HelpDescription => "show this text";

        public async Task<string> HandleCommand(TeamSpeakClient queryClient, int invokerId, string arguments)
        {
            var message = "Available Commands:\n";

            // Cycle through all commands and print the help line
            foreach (var cmd in ChatCommandList.Commands.OrderBy(k => k.Name))
            {
                // Dont list commands without HelpDescription
                if (cmd.HelpDescription == string.Empty)
                {
                    continue;
                }

                message += $"!{cmd.Name} - {cmd.HelpDescription}\n";
            }

            return message;

            return "Available Commands:\n"
                            + "!continue - show next help page\n"
                            + "!kick [random/r, Optional Username] - kicks you, a random, or specified user\n"
                            + "!meme(s) - Get some funky fresh memes!\n!help - You know.\n + "
                            + "!rank - Returns your current timerank (if time tracking is enabled)\n"
                            + "!reloadconfig - Reload current config file\n"
                            + "!roll [Optional number] - Rolls a dice with six sides [Rolls with x sides]\n"
                            + "!saveconfig - Save current config file\n"
                            + "!shrug, !tableflip, !unflip - shrug and flip!";

            return new NotImplementedException().ToString();
        }
    }
}
