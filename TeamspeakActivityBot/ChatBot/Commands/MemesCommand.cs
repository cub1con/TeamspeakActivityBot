using System;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamspeakActivityBot.ChatBot.Commands.Abstraction;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.ChatBot.Commands
{
    public class MemesCommand : IChatCommand
    {
        public string[] Name => new string[] { "memes" };

        public string HelpDescription => "Get some funky fresh memes!";

        public async Task<string> HandleCommand(TeamSpeakClient queryClient, int invokerId, TextCommand command)
        {
            return Misc.Memes.Captions[new Random().Next(0, Misc.Memes.Captions.Length - 1)];
        }
    }
}
