using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamspeakActivityBot.ChatBot.Commands.Abstraction;
using TeamspeakActivityBot.Manager;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.ChatBot.Commands
{
    public class ConfigCommand : IChatCommand
    {
        public string[] Name => new string[] { "config", };

        public string HelpDescription => string.Empty;
        //public string HelpDescription => "[reload / save] reloads the config if no parameter given";

        public async Task<string> HandleCommand(TeamSpeakClient queryClient, int invokerId, TextCommand command)
        {
            switch (command.Argument)
            {
                case "save":
                    ConfigManager.Save();
                    return "Successfully saved config";

                case "":
                case "reload":
                default:
                    ConfigManager.Load();
                    return "Successfully reloaded config";
            }
        }
    }
}
