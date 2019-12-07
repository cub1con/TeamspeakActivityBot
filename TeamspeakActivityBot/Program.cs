using TeamspeakActivityBot.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Responses;

namespace TeamspeakActivityBot
{
    class Program
    {
        private static FileInfo CLIENTS_FILE = new FileInfo(Path.Combine(Environment.CurrentDirectory, "clients.json"));
        private static FileInfo CONFIG_FILE = new FileInfo(Path.Combine(Environment.CurrentDirectory, "config.json"));
        private static TimeSpan WW2DURATION = (new DateTime(1945, 9, 2) - new DateTime(1939, 9, 1));
        private const int MAX_CHANNEL_NAME_LENGTH = 40;

        private static ClientManager ClientManager;
        private static ConfigManager ConfigManager;

        static void Main(string[] args)
        {
#if DEBUG
            while (!System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Waiting for Debugger...");
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine("Debugger attached!");
#endif

            ClientManager = new ClientManager(CLIENTS_FILE);
            ConfigManager = new ConfigManager(CONFIG_FILE);

            try
            {
                Console.WriteLine("[Press any key to exit]");
                RunBot().Wait();
            }
            catch (Exception ex)
            {
                int depth = 0;
                do
                {
                    Console.WriteLine("Exception #{0}: {1}", ++depth, ex.Message);
                    if (ex.GetType() == typeof(QueryException))
                        Console.WriteLine("Error: {0}", ((QueryException)ex).Error.Message);
                    Console.WriteLine("Stacktrace: {0}", ex.StackTrace);
                    Console.WriteLine("===========================================");
                } while ((ex = ex.InnerException) != null);
            }
            Console.WriteLine("Done.");
        }

        private static async Task RunBot()
        {
            var bot = await GetConnectedClient();
            var lastUserStatsUpdate = DateTime.Now;
            var lastChannelUpdate = DateTime.MinValue;
            while (!Console.KeyAvailable)
            {
                if (DateTime.Now - lastUserStatsUpdate >= ConfigManager.Config.TimeLogInterval)
                {
                    await CollectOnlineTime(bot, lastUserStatsUpdate);
                    lastUserStatsUpdate = DateTime.Now;
                }
                if (DateTime.Now - lastChannelUpdate >= ConfigManager.Config.ChannelUpdateInterval)
                {
                    await SetTopList(bot);
                    lastChannelUpdate = DateTime.Now;
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static async Task<TeamSpeakClient> GetConnectedClient()
        {
            var bot = new TeamSpeakClient(ConfigManager.Config.Host, ConfigManager.Config.Port);
            await bot.Connect();
            await bot.Login(ConfigManager.Config.QueryUsername, ConfigManager.Config.QueryPassword);
            await bot.UseServer((await bot.GetServers()).FirstOrDefault().Id);
            return bot;
        }

        private static async Task SetTopList(TeamSpeakClient bot)
        {
            if (!ClientManager.Clients.Any())
            {
                Console.WriteLine("[!] Couldn't update channel info: no users! ==========");
                return;
            }

            Console.WriteLine("[>] Updating channel info");
            var topUsers = ClientManager.Clients.OrderByDescending(x => x.ActiveTime).ToArray();
            var channelName = FormatChannelName(topUsers.FirstOrDefault()); ;

            var channelInfo = await bot.GetChannelInfo(ConfigManager.Config.ChannelId);
            var editInfo = new EditChannelInfo();

            editInfo.Description = FormatChannelDescription(topUsers);
            if (channelInfo.Name != channelName)
                editInfo.Name = channelName;
            await bot.EditChannel(ConfigManager.Config.ChannelId, editInfo);

        }

        private static string FormatChannelDescription(Client[] topUsers)
        {
            var totalTime = TimeSpan.FromTicks(topUsers.Sum(x => x.ActiveTime.Ticks));
            var description = new StringBuilder();
            description.AppendLine($"Seit {ConfigManager.Config.LoggingSince}:");
            description.AppendLine(string.Join(Environment.NewLine, topUsers.Select(c => c.ToString()).ToArray()));
            description.AppendLine("Fun facts:");
            description.AppendLine(string.Format(
                "-> Insgesamt verschwendete Zeit: {0}",
                totalTime.ToString(@"ddd\T\ hh\:mm\:ss")));
            description.AppendLine(string.Format(
                "-> Damit hätten wir {0} mal den 2. Weltkrieg führen können!",
                ((double)totalTime.Ticks / (double)WW2DURATION.Ticks).ToString("0.000")));
            description.Append(string.Format(
                "-> Durchschnittlich verschwendete Zeit: {0}",
                TimeSpan.FromTicks(totalTime.Ticks / topUsers.Length).ToString(@"ddd\T\ hh\:mm\:ss")));
            return description.ToString();
        }

        private static string FormatChannelName(Client topUser)
        {
            var channelName = ConfigManager.Config.ChannelNameFormat.Replace("%NAME%", topUser.DisplayName);
            if (channelName.Length > MAX_CHANNEL_NAME_LENGTH)
            {
                var maxNameLength = ConfigManager.Config.ChannelNameFormat.Length - "%NAME%".Length;
                var userName = topUser.DisplayName;
                if (userName.Contains("|") && userName.IndexOf('|') <= maxNameLength)
                    userName = userName.Substring(0, userName.IndexOf('|')).Trim();
                else
                    userName = userName.Substring(0, maxNameLength).Trim();
                channelName = ConfigManager.Config.ChannelNameFormat.Replace("%NAME%", userName);
            }
            return channelName;
        }

        private static async Task CollectOnlineTime(TeamSpeakClient bot, DateTime lastRun)
        {
            Console.WriteLine("[>] Collecting online time");
            var clients = await bot.GetClients();
            
            var clientInfos = new List<GetClientDetailedInfo>();
            foreach (var cl in clients.Where(c => c.Type == ClientType.FullClient)) 
                clientInfos.Add(await bot.GetClientInfo(cl.Id));
           
            var trackedClients = new List<GetClientDetailedInfo>();
            foreach (var cl in clientInfos.Where(c => c.ServerGroupIds.Any(id => ConfigManager.Config.UserGroups.Contains(id)))) 
                trackedClients.Add(cl);
            
            bool anyChange = false;
            
            foreach (var ci in trackedClients) anyChange |= UpdateClientTime(lastRun, ci);
            if (anyChange)
                ClientManager.Save();
        }

        private static bool UpdateClientTime(DateTime lastRun, GetClientDetailedInfo clientInfo)
        {
            var client = ClientManager[clientInfo.DatabaseId.ToString()];
            if (client == null)
            {
                client = ClientManager.AddClient(new Client()
                {
                    ClientId = clientInfo.DatabaseId.ToString(),
                    DisplayName = clientInfo.NickName,
                    ActiveTime = TimeSpan.Zero
                });
            }

            var conditionAway = (!clientInfo.Away && !ConfigManager.Config.LogAFK);
            var conditionMuted = (!clientInfo.IsOutputMuted() && !ConfigManager.Config.LogOutputMuted);

            if ((conditionAway && conditionMuted) && clientInfo.IdleTime < ConfigManager.Config.MaxIdleTime)
            {
                client.ActiveTime += (DateTime.Now - lastRun);
                return true;
            }

            return false;
        }
    }
}
