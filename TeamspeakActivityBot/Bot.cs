using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamspeakActivityBot.Extensions;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Manager;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot
{
    public class Bot
    {
        private static readonly TimeSpan WW2DURATION = (new DateTime(1945, 9, 2) - new DateTime(1939, 9, 1));
        private const int MAX_CHANNEL_NAME_LENGTH = 40;

        private ClientManager clientManager;

        private ConfigManager configManager;

        private TeamSpeakClient queryClient;

        private WhoAmI queryClientInfo;

        public Bot(ClientManager cManager, ConfigManager cfgManager)
        {
            this.clientManager = cManager;
            this.configManager = cfgManager;
        }

        private async void HandleServerChatMessages(IReadOnlyCollection<TextMessage> textMessages)
        {
            foreach (var msg in textMessages)
            {
                // Prevent reacting to own messages
                if (msg.InvokerId == queryClientInfo.OriginServerId || !msg.Message.StartsWith('!'))
                {
                    break;
                }

                await BotHandler.TextCommandHandler.HandleMessage(msg, queryClient);
            }
        }

        public async Task Run()
        {
            // Get all connected clients
            this.queryClient = await GetConnectedClient();
            this.queryClientInfo = await queryClient.WhoAmI();

            var lastUserStatsUpdate = DateTime.Now;
            var lastChannelUpdate = DateTime.MinValue;

            LogHelper.LogUpdate("Activated features:");
            LogHelper.LogUpdate($" - TrackClientActiveTimes: {configManager.Config.TrackClientActiveTimes}");
            LogHelper.LogUpdate($" - TrackClientConnectedTimes: {configManager.Config.TrackClientConnectedTimes}");
            LogHelper.LogUpdate($" - TopListUpdateChannel: {configManager.Config.TopListUpdateChannel}");
            LogHelper.LogUpdate($" - EnableChatCommands: {configManager.Config.ChatCommandsEnabled}");


            // If chat commands are enabled, subscribe to updates
            if (this.configManager.Config.EnableChatCommands)
            {
                // register server wide text notifications
                await this.queryClient.RegisterTextServerNotification();

                queryClient.Subscribe<TextMessage>(HandleServerChatMessages);
            }


            while (!Console.KeyAvailable)
            {
                // Collect ClientTimes after timespan if option is enabled
                if (DateTime.Now - lastUserStatsUpdate >= configManager.Config.TrackTimeLogInterval && configManager.Config.TrackClientTimes && false)
                {
                    await CollectClientTimes(lastUserStatsUpdate);
                    lastUserStatsUpdate = DateTime.Now;
                }
                // Update the TopListChannel with toplist in description and MVP in channel name
                if (DateTime.Now - lastChannelUpdate >= configManager.Config.TopListChannelUpdateInterval && configManager.Config.TopListUpdateChannel && false)
                {
                    await UpdateTopListChannel();
                    lastChannelUpdate = DateTime.Now;
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private async Task<TeamSpeakClient> GetConnectedClient()
        {
            // Create the actual client, connect, login and choose default instance
            var bot = new TeamSpeakClient(configManager.Config.Host, configManager.Config.HostPort);
            await bot.Connect();
            await bot.Login(configManager.Config.QueryUsername, configManager.Config.QueryPassword);

            // Default fresh installed Server Instance is 1, but we query it from the server because it might be not 1
            if (configManager.Config.QueryInstanceId != 0)
            {
                await bot.UseServer(configManager.Config.QueryInstanceId);
            }
            else
            {
                LogHelper.LogWarning("No Instance configured, fallback to query.");
                await bot.UseServer((await bot.GetServers()).FirstOrDefault().Id);
            }

            return bot;
        }

        private async Task UpdateTopListChannel()
        {
            if (!clientManager.Clients.Data.Any())
            {
                LogHelper.LogWarning("Couldn't update channel info: no users!");
                return;
            }

            LogHelper.LogUpdate("Updating channel info");

            // Get users ordered DESC by the ActiveTime
            var clients = clientManager.Clients.Data.ToArray();
            var channelName = FormatChannelName(clients.OrderByDescending(x => x.ActiveTime).FirstOrDefault());

            // Create the channel top list description
            var description = FormatChannelDescription(clients);
            await this.queryClient.EditChannel(configManager.Config.TopListChannelId, ChannelEdit.channel_description, description);

            // Update channel name with mvp, if name is not identical
            var channelInfo = await this.queryClient.GetChannelInfo(configManager.Config.TopListChannelId);
            if (channelInfo.Name != channelName)
                await this.queryClient.EditChannel(configManager.Config.TopListChannelId, ChannelEdit.channel_name, channelName);
        }

        private string FormatChannelDescription(Client[] clients)
        {
            // StringBuilder to hold the channel description
            var sb = new StringBuilder();
            sb.AppendLine($"Seit {configManager.Config.TrackLoggingSince}:");

            // Format for TopUsers
            var topUsers = clients.OrderByDescending(x => x.ActiveTime).ToArray();

            var totalTimeTop = TimeSpan.FromTicks(topUsers.Sum(x => x.ActiveTime.Ticks));
            sb.AppendLine($"AKTIV:");
            sb.AppendLine(string.Join(Environment.NewLine, topUsers.Select(c => c.ToString()).ToArray()));
            sb.AppendLine("Fun facts:");
            sb.AppendLine($"-> Insgesamt verschwendete Zeit: {totalTimeTop.ToString(@"ddd\T\ hh\:mm\:ss")}");
            sb.AppendLine(string.Format(
                "-> Damit hätten wir {0} mal den 2. Weltkrieg führen können!",
                ((double)totalTimeTop.Ticks / (double)WW2DURATION.Ticks).ToString("0.000")));
            sb.AppendLine(string.Format(
                "-> Durchschnittlich verschwendete Zeit: {0}",
                TimeSpan.FromTicks(totalTimeTop.Ticks / topUsers.Length).ToString(@"ddd\T\ hh\:mm\:ss")));

            sb.AppendLine(Environment.NewLine);

            // Format for all users TODO: Make this optional?
            var completeUsers = clients.OrderByDescending(x => x.ConnectedTime).ToArray();

            var totalTimeAll = TimeSpan.FromTicks(topUsers.Sum(x => x.ConnectedTime.Ticks));
            sb.AppendLine($"VERBUNDEN:");
            sb.AppendLine(string.Join(Environment.NewLine, completeUsers.Select(c => c.ToConnectedTimeString()).ToArray()));
            sb.AppendLine("Fun facts:");
            sb.AppendLine($"-> Insgesamt verbundene Zeit: {totalTimeAll.ToString(@"ddd\T\ hh\:mm\:ss")}");
            sb.AppendLine(string.Format(
                "-> Damit hätten wir {0} mal den 2. Weltkrieg führen können!",
                ((double)totalTimeAll.Ticks / (double)WW2DURATION.Ticks).ToString("0.000")));
            sb.Append(string.Format(
                "-> Durchschnittlich verbundene Zeit: {0}",
                TimeSpan.FromTicks(totalTimeAll.Ticks / completeUsers.Length).ToString(@"ddd\T\ hh\:mm\:ss")));

            return sb.ToString();
        }

        /// <summary>
        /// Formats the TopListChannelName with the topUser name
        /// </summary>
        /// <param name="topUser">Client object of the topUser</param>
        /// <returns></returns>
        private string FormatChannelName(Client topUser)
        {
            // Get the template name and fill in the Client.DisplayName
            var channelName = configManager.Config.TopListChannelNameFormat.Replace("%NAME%", topUser.DisplayName);

            // ChannelName to long for TeamSpeak?
            if (channelName.Length > MAX_CHANNEL_NAME_LENGTH)
            {
                // Get max available username length for wildcard
                var maxNameLength = configManager.Config.TopListChannelNameFormat.Length - "%NAME%".Length;
                var userName = topUser.DisplayName;

                // If topUser.DisplayName contains a '|' (pipe), cut it in front of the pipe
                // else just get the name trimed to the max available username length
                if (userName.Contains('|') && userName.IndexOf('|') <= maxNameLength)
                    userName = userName.Substring(0, userName.IndexOf('|')).Trim();
                else
                    userName = userName.Substring(0, maxNameLength).Trim();
                channelName = configManager.Config.TopListChannelNameFormat.Replace("%NAME%", userName);
            }
            return channelName;
        }

        private async Task CollectClientTimes(DateTime lastRun)
        {
            // TODO: Add detection if user is alone in channel, then stop collect active time
            LogHelper.LogUpdate("Collecting online time");
            var clients = await this.queryClient.GetClients();

            // Get a ClientInfo for every connected user
            var clientInfos = new List<GetClientDetailedInfo>();
            foreach (var cl in clients.Where(c => c.Type == ClientType.FullClient))
                clientInfos.Add(await this.queryClient.GetClientInfo(cl.Id));

            // Only get trackedClients
            var trackedClients = new List<GetClientDetailedInfo>();
            foreach (var cl in clientInfos.Where(c =>
                !c.ServerGroupIds.Any(id => this.configManager.Config.TrackIgnoreUserGroups.Contains(id) &&     // Ignore User if in specified group
                c.ServerGroupIds.Any(id => this.configManager.Config.TrackUserGroups.Contains(id))) &&               // Ignore user if not in specified usergroups
                !configManager.Config.TrackIgnoreChannels.Contains(c.ChannelId)))                                    // Ignore User if in specified channels
                trackedClients.Add(cl);

            bool anyChange = false;

            foreach (var ci in trackedClients) anyChange |= UpdateClientTime(lastRun, ci);
            if (anyChange)
                clientManager.Clients.Save();
        }

        private bool UpdateClientTime(DateTime lastRun, GetClientDetailedInfo clientInfo)
        {
            bool update = false;
            var calculatedTime = (DateTime.Now - lastRun);

            var client = clientManager[clientInfo.DatabaseId];
            if (client == null)
            {
                client = clientManager.AddClient(new Client()
                {
                    ClientId = clientInfo.DatabaseId,
                    DisplayName = clientInfo.NickName,
                    ActiveTime = TimeSpan.Zero,
                    ConnectedTime = TimeSpan.Zero
                });
            }

            // Track total time
            if (configManager.Config.TrackClientConnectedTimes)
            {
                client.ConnectedTime += calculatedTime;
                update = true;
            }

            // If we don't collect active time, we can return
            if (!configManager.Config.TrackClientActiveTimes)
            {
                return update;
            }

            // Track active Time
            // Ignore user if afk
            var conditionNotAway = !clientInfo.Away && !configManager.Config.TrackAFK;

            // Ignore user if muted
            var conditionNotMuted = !clientInfo.IsInputOrOutputMuted() && !configManager.Config.TrackOutputMuted;

            // Ignore user if idle is longer than threshold
            var conditionIdleTIme = clientInfo.IdleTime < configManager.Config.TrackMaxIdleTime;

            if (conditionNotAway && conditionNotMuted && conditionIdleTIme)
            {
                client.ActiveTime += calculatedTime;
                update = true;
            }

            return update;
        }
    }
}

