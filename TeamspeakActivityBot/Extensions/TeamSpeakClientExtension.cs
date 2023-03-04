using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Responses;

namespace TeamspeakActivityBot.Extensions
{
    public static class TeamSpeakClientExtension
    {
        /// <summary>
        /// Returns only full clients, ignores querys
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<GetClientInfo>> GetFullClients(this TeamSpeakClient client)
        {
            var clients = await client.GetClients();

            if (clients == null)
                return new List<GetClientInfo>();

            return clients.Where(x => x.Type == ClientType.FullClient);
            // normally we would do this, but the TeamSpeakClient breaks if access it like this
            //return (await client.GetClients()).Where(x => x.Type == ClientType.FullClient);
        }

        public static async Task<List<GetClientDetailedInfo>> GetFullClientsDetailedInfo(this TeamSpeakClient client)
        {
            var returnList = new List<GetClientDetailedInfo>();
            foreach (var clientInfo in await client.GetFullClients())
            {
                try
                {
                    returnList.Add(await client.GetClientInfo(clientInfo.Id));
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Trace(ex, "User disconnected before getting client info");
                }
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
    }
}
