using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Chat.Commands.Abstraction
{
    public interface IChatCommand
    {
        /// <summary>
        /// An array of strings, to enable multiple functions, by which the command gets accessed
        /// </summary>
        public string[] Name { get; }

        /// <summary>
        /// A description of the command with possible arguments
        /// Gets accessed by the HelpCommand
        /// </summary>
        public string HelpDescription { get; }

        /// <summary>
        /// Gets called if the command is selected
        /// </summary>
        /// <param name="queryClient">The currently connected <see cref="TeamSpeakClient"/></param>
        /// <param name="invokerId">The userId of the invoker</param>
        /// <param name="command">The <see cref="TextCommand"/> with the command and argument</param>
        /// <returns></returns>
        public Task<string> HandleCommand(TeamSpeakClient queryClient, int invokerId, TextCommand command);
    }
}
