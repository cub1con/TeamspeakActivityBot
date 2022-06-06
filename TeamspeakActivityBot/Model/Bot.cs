using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamspeakActivityBot.Extensions;
using TeamspeakActivityBot.Manager;

namespace TeamspeakActivityBot.Model
{
    public class Bot
    {
        private static TimeSpan WW2DURATION = (new DateTime(1945, 9, 2) - new DateTime(1939, 9, 1));
        private const int MAX_CHANNEL_NAME_LENGTH = 40;

        private ClientManager clientManager;

        private ConfigManager configManager;

        public Bot(ClientManager cManager, ConfigManager cfgManager)
        {
            this.clientManager = cManager;
            this.configManager = cfgManager;
        }

        public async Task Run()
        {
            // Get all connected clients
            var bot = await GetConnectedClient();
            var lastUserStatsUpdate = DateTime.Now;
            var lastChannelUpdate = DateTime.MinValue;
            while (!Console.KeyAvailable)
            {
                if (DateTime.Now - lastUserStatsUpdate >= configManager.Config.TimeLogInterval)
                {
                    await CollectOnlineTime(bot, lastUserStatsUpdate);
                    lastUserStatsUpdate = DateTime.Now;
                }
                if (DateTime.Now - lastChannelUpdate >= configManager.Config.ChannelUpdateInterval)
                {
                    await SetTopList(bot);
                    lastChannelUpdate = DateTime.Now;
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private async Task<TeamSpeakClient> GetConnectedClient()
        {
            var bot = new TeamSpeakClient(configManager.Config.Host, configManager.Config.Port);
            await bot.Connect();
            await bot.Login(configManager.Config.QueryUsername, configManager.Config.QueryPassword);
            await bot.UseServer((await bot.GetServers()).FirstOrDefault().Id);
            return bot;
        }

        private async Task SetTopList(TeamSpeakClient bot)
        {
            if (!clientManager.Clients.Data.Any())
            {
                Console.WriteLine("[!] Couldn't update channel info: no users! ==========");
                return;
            }

            Console.WriteLine("[>] Updating channel info");
            var topUsers = clientManager.Clients.Data.OrderByDescending(x => x.ActiveTime).ToArray();
            var channelName = FormatChannelName(topUsers.FirstOrDefault()); ;

            var channelInfo = await bot.GetChannelInfo(configManager.Config.ChannelId);

            var description = FormatChannelDescription(topUsers);
            await bot.EditChannel(configManager.Config.ChannelId, ChannelEdit.channel_description, description);

            if (channelInfo.Name != channelName)
                await bot.EditChannel(configManager.Config.ChannelId, ChannelEdit.channel_name, channelName);


        }

        private string FormatChannelDescription(Client[] topUsers)
        {
            var totalTime = TimeSpan.FromTicks(topUsers.Sum(x => x.ActiveTime.Ticks));
            var description = new StringBuilder();
            description.AppendLine($"Seit {configManager.Config.LoggingSince}:");
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

        private string FormatChannelName(Client topUser)
        {
            var channelName = configManager.Config.ChannelNameFormat.Replace("%NAME%", topUser.DisplayName);
            if (channelName.Length > MAX_CHANNEL_NAME_LENGTH)
            {
                var maxNameLength = configManager.Config.ChannelNameFormat.Length - "%NAME%".Length;
                var userName = topUser.DisplayName;
                if (userName.Contains("|") && userName.IndexOf('|') <= maxNameLength)
                    userName = userName.Substring(0, userName.IndexOf('|')).Trim();
                else
                    userName = userName.Substring(0, maxNameLength).Trim();
                channelName = configManager.Config.ChannelNameFormat.Replace("%NAME%", userName);
            }
            return channelName;
        }

        private async Task CollectOnlineTime(TeamSpeakClient bot, DateTime lastRun)
        {
            Console.WriteLine("[>] Collecting online time");
            var clients = await bot.GetClients();

            var clientInfos = new List<GetClientDetailedInfo>();
            foreach (var cl in clients.Where(c => c.Type == ClientType.FullClient))
                clientInfos.Add(await bot.GetClientInfo(cl.Id));

            var trackedClients = new List<GetClientDetailedInfo>();
            foreach (var cl in clientInfos.Where(c => c.ServerGroupIds.Any(id => configManager.Config.UserGroups.Contains(id))))
                trackedClients.Add(cl);

            bool anyChange = false;

            foreach (var ci in trackedClients) anyChange |= UpdateClientTime(lastRun, ci);
            if (anyChange)
                clientManager.Clients.Save();
        }

        private bool UpdateClientTime(DateTime lastRun, GetClientDetailedInfo clientInfo)
        {
            var client = clientManager[clientInfo.DatabaseId.ToString()];
            if (client == null)
            {
                client = clientManager.AddClient(new Client()
                {
                    ClientId = clientInfo.DatabaseId.ToString(),
                    DisplayName = clientInfo.NickName,
                    ActiveTime = TimeSpan.Zero
                });
            }

            var conditionAway = (!clientInfo.Away && !configManager.Config.LogAFK);
            var conditionMuted = (!clientInfo.IsOutputMuted() && !configManager.Config.LogOutputMuted);

            if ((conditionAway && conditionMuted) && clientInfo.IdleTime < configManager.Config.MaxIdleTime)
            {
                client.ActiveTime += (DateTime.Now - lastRun);
                return true;
            }

            return false;
        }
    }
}

