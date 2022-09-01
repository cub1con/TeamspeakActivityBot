using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamspeakActivityBot.Manager;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Extensions
{
    public static class TeamSpeakClientExtension
    {
        public static async Task<IEnumerable<GetClientInfo>> GetFullClients(this TeamSpeakClient client)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Trace("Before getting FullClients");

            var clients = await client.GetClients();

            //var clients = await RunSafe(client.GetClients()); // client.GetClients().WaitAsync(TimeSpan.FromSeconds(5));
            
            if (clients == null)
                return new List<GetClientInfo>();

            logger.Trace("Before returning fullclients");
            return clients.Where(x => x.Type == ClientType.FullClient);
            // normally we would do this, but the TeamSpeakClient breaks if access it like this
            //return (await client.GetClients()).Where(x => x.Type == ClientType.FullClient);
        }

        /// <summary>
        /// This is is a fucked up function to prevent a deadlock from TeamSpeak3QueryApi.Net.Specialized.TeamSpeakClient
        /// If you query to fast it locks up completly
        /// It runs the task for the <paramref name="timeout"/> (default 3s) and disposes it if it didn't finsh
        /// </summary>
        /// <typeparam name="T">A <typeparamref name="T"/> which should not lock up!</typeparam>
        /// <param name="task">The Task of <typeparamref name="T"/> you want not to lock up</param>
        /// <param name="timeout">The defined <paramref name="timeout"/></param>
        /// <returns></returns>
        public static async Task<T> RunSafe<T>(this Task<T> task, TimeSpan? timeout = null)
        {
            if (timeout == null) timeout = TimeSpan.FromSeconds(3);

            var logger = NLog.LogManager.GetCurrentClassLogger();
            var now = DateTime.Now;
            while (true)
            {
                logger.Trace($"IsCompleted: {task.IsCompleted}");
                if (task.IsCompleted)
                {
                    logger.Debug($"{typeof(T).Namespace} completed");
                    return task.Result;
                }

                if (DateTime.Now.Ticks > now.AddTicks(timeout.Value.Ticks).Ticks)
                {
                    logger.Error($"{typeof(T)} ran out of time");
                    task = null;
                    return default;
                }

                await Task.Delay(10);
            }
        }

        //public static async Task<User> GetUserByUid(this TeamSpeakClient client, string uid)
        //{
        //    var currenClients = await client.GetClients();
        //    var user = currenClients.First(x => x.)

        //    return client.GetClientInfo(tmp);
        //}

        public static async Task<List<GetClientDetailedInfo>> GetFullClientsDetailedInfo(this TeamSpeakClient client)
        {
            //var logger = NLog.LogManager.GetCurrentClassLogger();
            //logger.Trace("Before getting FullClientDetailedInfo");

            var returnList = new List<GetClientDetailedInfo>();
            foreach (var clientInfo in await client.GetFullClients())
            {
                returnList.Add(await client.GetClientInfo(clientInfo.Id));
            }

            //logger.Trace("Before returning FullClientDetailedInfo");
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

        public static async Task<List<GetClientDetailedInfo>> GetFilteredClients(this TeamSpeakClient client)
        {
            var returnList = new List<GetClientDetailedInfo>();

            // Get a ClientInfo for every connected user
            foreach (var cl in await client.GetFullClientsDetailedInfo())
            {
                // Check if User is in an ignored group, break
                if (cl.ServerGroupIds.Any(id => ConfigManager.Config.TrackIgnoreUserGroups.Contains(id)))
                {
                    continue;
                }

                // Check if User is in an Tracked group, continue
                if (!cl.ServerGroupIds.Any(id => ConfigManager.Config.TrackUserGroups.Contains(id)))
                {
                    continue;
                }

                // Check if User is in an ignored channel, break
                if (ConfigManager.Config.TrackIgnoreChannels.Contains(cl.ChannelId))
                {
                    continue;
                }
                returnList.Add(cl);
            }
            return returnList;
        }
    }
}
