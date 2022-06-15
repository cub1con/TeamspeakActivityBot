using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Responses;

namespace TeamspeakActivityBot.Extensions
{
    public static class TeamSpeakClientExtension
    {
        public static async Task<GetClientInfo[]> GetFullClients(this TeamSpeakClient client)
        {
            return (await client.GetClients()).Where(x => x.Type == ClientType.FullClient).ToArray();            
        }
    }
}
