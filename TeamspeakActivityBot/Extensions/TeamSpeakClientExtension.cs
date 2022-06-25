using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamspeakActivityBot.Manager;

namespace TeamspeakActivityBot.Extensions
{
    public static class TeamSpeakClientExtension
    {
        public static async Task<IEnumerable<GetClientInfo>> GetFullClients(this TeamSpeakClient client)
        {
            return (await client.GetClients()).Where(x => x.Type == ClientType.FullClient);
        }

        public static async Task<List<GetClientDetailedInfo>> GetFullClientsDetailedInfo(this TeamSpeakClient client)
        {
            var returnList = new List<GetClientDetailedInfo>();
            foreach (var clientInfo in await client.GetFullClients())
            {
                returnList.Add(await client.GetClientInfo(clientInfo.Id));
            }
            return returnList;
        }

        public static async Task<GetClientInfo> GetUserByID(this TeamSpeakClient client, int id)
        {
            return (await client.GetFullClients()).FirstOrDefault(x => x.Id == id);
        }

        public static async Task<GetClientInfo> GetUserByDbID(this TeamSpeakClient client, int id)
        {
            return (await client.GetFullClients()).FirstOrDefault(x => x.DatabaseId == id);
        }

        public static async Task<List<GetClientDetailedInfo>> GetFilteredClients(this TeamSpeakClient client, ConfigManager configManager)
        {
            var returnList = new List<GetClientDetailedInfo>();

            // Get a ClientInfo for every connected user
            foreach (var cl in await client.GetFullClientsDetailedInfo())
            {
                // Check if User is in an ignored group, break
                if (cl.ServerGroupIds.Any(id => configManager.Config.TrackIgnoreUserGroups.Contains(id)))
                {
                    continue;
                }

                // Check if User is in an Tracked group, continue
                if (!cl.ServerGroupIds.Any(id => configManager.Config.TrackUserGroups.Contains(id)))
                {
                    continue;
                }

                // Check if User is in an ignored channel, break
                if (configManager.Config.TrackIgnoreChannels.Contains(cl.ChannelId))
                {
                    continue;
                }
                returnList.Add(cl);
            }
            return returnList;
        }
    }
}
