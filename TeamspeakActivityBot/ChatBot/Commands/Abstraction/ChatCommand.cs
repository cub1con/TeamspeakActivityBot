using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;

namespace TeamspeakActivityBot.ChatBot.Commands.Abstraction
{
    public interface ChatCommand
    {
        public string[] Name { get; }

        public string HelpDescription { get; }

        public Task<string> HandleCommand(TeamSpeakClient queryClient, int invokerId, string arguments);
    }
}
