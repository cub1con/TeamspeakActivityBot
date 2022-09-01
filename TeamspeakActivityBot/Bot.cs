using NLog;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamspeakActivityBot.Chat;
using TeamspeakActivityBot.Exceptions;
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

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private TeamSpeakClient queryClient;

        //private System.Threading.Timer timerThread;

        public Bot()
        {
            Logger.Info("Activated features:");
            Logger.Info($" - TrackClientActiveTimes: {ConfigManager.Config.TrackClientActiveTimes}");
            Logger.Info($" - TrackClientConnectedTimes: {ConfigManager.Config.TrackClientConnectedTimes}");
            Logger.Info($" - TopListUpdateChannel: {ConfigManager.Config.TopListUpdateChannel}");
            Logger.Info($" - EnableChatCommands: {ConfigManager.Config.ChatCommandsEnabled}");
        }

        public async Task Run()
        {
            Logger.Info("Starting bot...");
            int retryCounter = 0;

            // Main loop, restart client on connection loss
            while (retryCounter < 3 && !Console.KeyAvailable)
            {
                try
                {
                    // Get connected client and identity
                    this.queryClient = await GetConnectedClient();


                    if (!this.queryClient.Client.IsConnected || (await this.queryClient.WhoAmI()).VirtualServerStatus == "unknown")
                    {
                        throw new NotConnectedException();
                    }

                    await this.queryClient.ChangeNickName(ConfigManager.Config.BotName);


                    // TODO: This currently breaks the time collecting loop
                    // i guess it's getting accessed while its still busy in a synchronous loop

                    // If chat commands are enabled, subscribe to updates
                    // Those only work in global chat tho
                    if (ConfigManager.Config.ChatCommandsEnabled)
                    {
                        // register server wide text notifications
                        await this.queryClient.RegisterTextServerNotification();

                        // Here would be code to register all channels, but this
                        // is not possible, because the queryClient has to be
                        // in the channel to get the notification
                        // await queryClient.RegisterTextChannelNotification();

                        this.queryClient.Subscribe<TextMessage>((o) => { ChatBot.HandleServerChatMessages(o, this.queryClient); });
                    }

                    Logger.Info("Bot connected...");


                    var lastUserStatsUpdate = DateTime.Now;
                    var lastChannelUpdate = DateTime.MinValue;

                    // TODO: Add logic to handle connectionloss and reconnect
                    while (!Console.KeyAvailable)
                    {
                        // Collect ClientTimes after timespan if option is enabled
                        if (DateTime.Now - lastUserStatsUpdate >= ConfigManager.Config.TrackTimeLogInterval && ConfigManager.Config.TrackClientTimes)
                        {
                            await CollectClientTimes(lastUserStatsUpdate);
                            lastUserStatsUpdate = DateTime.Now;
                        }
                        // Update the TopListChannel with toplist in description and MVP in channel name
                        if (DateTime.Now - lastChannelUpdate >= ConfigManager.Config.TopListChannelUpdateInterval && ConfigManager.Config.TopListUpdateChannel)
                        {
                            await UpdateTopListChannel();
                            lastChannelUpdate = DateTime.Now;
                        }

                        // https://docs.microsoft.com/en-us/dotnet/api/system.threading.periodictimer?view=net-6.0
                        // https://stackoverflow.com/questions/30462079/run-async-method-regularly-with-specified-interval#:~:text=FromMinutes(5))%3B-,.NET%206%20update,-%3A%20It%20is
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }
                catch (Exception ex)
                {
                    retryCounter++;
                    
                    if (ex.GetType() == typeof(NotConnectedException))
                    {
                        Logger.Warn("Could not connect");
                        throw;
                    }

                    if (ex.GetType() == typeof(QueryException))
                    {
                        ExceptionHelper.HandleBotException(ex);
                        Logger.Warn("Retrying in 60 seconds...");
                        this.queryClient?.Dispose();
                        await Task.Delay(TimeSpan.FromSeconds(60));
                        continue;
                    }

                    ExceptionHelper.HandleBotException(ex);
                    continue;
                }

                retryCounter = int.MaxValue;
            }
        }

        private static async Task<TeamSpeakClient> GetConnectedClient()
        {
            Logger.Debug("Creating Client...");
            // Create the client
            var bot = new TeamSpeakClient(ConfigManager.Config.Host, ConfigManager.Config.HostPort, TimeSpan.FromMinutes(1));

            // Connect client to server
            // Errors produced here are from server
            await bot.Connect();
            await bot.Login(ConfigManager.Config.QueryUsername, ConfigManager.Config.QueryPassword);

            // Choose instance to connect
            // Default fresh installed Server Instance is 1, but we query it from the server because it might be not 1
            // Errors produced after here are probably instance related, but could be server related
            if (ConfigManager.Config.QueryInstanceId != 0)
            {
                await bot.UseServer(ConfigManager.Config.QueryInstanceId);
            }
            else
            {
                Logger.Info("No Instance configured, fallback to query");
                var availableInstances = (await bot.GetServers()).ToArray();
                await bot.UseServer(availableInstances.FirstOrDefault().Id);
            }

            return bot;
        }

        private async Task UpdateTopListChannel()
        {
            if (!UserManager.Users.Any())
            {
                Logger.Warn("Couldn't update channel info: no users!");
                return;
            }

            Logger.Info("Updating channel info");

            // Get users ordered DESC by the ActiveTime
            var clients = UserManager.Users.ToArray();

            // Get topListChannel info
            var topListChannelInfo = await this.queryClient.GetChannelInfo(ConfigManager.Config.TopListChannelId);


            // Get the channel leaderboard
            var newChannelDescription = GetFormatedChannelDescription(clients);
            // Only update the channel if there is a difference
            if (topListChannelInfo.Description != newChannelDescription)
            {
                Logger.Trace("Editing channel description");
                await this.queryClient.EditChannel(ConfigManager.Config.TopListChannelId, ChannelEdit.channel_description, newChannelDescription);
            }

            // Get new channel name
            var channelName = GetFormatedChannelName(clients.OrderByDescending(x => x.ActiveTime).FirstOrDefault());
            // Update channel name with mvp, if name is not identical
            if (topListChannelInfo.Name != channelName)
            {
                Logger.Trace("Editing channel name");
                await this.queryClient.EditChannel(ConfigManager.Config.TopListChannelId, ChannelEdit.channel_name, channelName);
            }

            Logger.Trace("Finished Updating channel info");
        }

        private static string GetFormatedChannelDescription(User[] clients)
        {
            Logger.Trace("Started formatting channel description");

            // StringBuilder to hold the channel description
            var sb = new StringBuilder();
            sb.AppendLine($"Seit {ConfigManager.Config.TrackLoggingSince}:");

            // Only select first 10 users
            // Format for TopUsers
            var clientsActiveTimeFirst10 = clients.OrderByDescending(x => x.ActiveTime).Take(10).ToArray();

            var clientsActiveTimeTotal = TimeSpan.FromTicks(clients.Sum(x => x.ActiveTime.Ticks));
            sb.AppendLine($"AKTIV:");
            sb.AppendLine(string.Join(Environment.NewLine, clientsActiveTimeFirst10.Select(c => c.GetActiveTimeAndName()).ToArray()));
            sb.AppendLine("Fun facts:");
            sb.AppendLine($"-> Insgesamt verschwendete Zeit: {clientsActiveTimeTotal.ToString(@"ddd\T\ hh\:mm\:ss")}");
            sb.AppendLine(string.Format(
                "-> Damit hätten wir {0} mal den 2. Weltkrieg führen können!",
                ((double)clientsActiveTimeTotal.Ticks / (double)WW2DURATION.Ticks).ToString("0.000")));
            sb.AppendLine(string.Format(
                "-> Durchschnittlich verschwendete Zeit: {0}",
                TimeSpan.FromTicks(clientsActiveTimeTotal.Ticks / clients.Length).ToString(@"ddd\T\ hh\:mm\:ss")));

            sb.AppendLine(Environment.NewLine);

            // Format for all users TODO: Make this optional?
            var clientsCompleteTimeFirst10 = clients.OrderByDescending(x => x.TotalTime).Take(10).ToArray();

            var clientsCompleteTimeTotal = TimeSpan.FromTicks(clients.Sum(x => x.TotalTime.Ticks));
            sb.AppendLine($"VERBUNDEN:");
            sb.AppendLine($"ZEIT - USERNAME - AKTIV RATIO");
            sb.AppendLine(string.Join(Environment.NewLine, clientsCompleteTimeFirst10.Select(c => c.GetTotalTimeAndName()).ToArray()));
            sb.AppendLine("Fun facts:");
            sb.AppendLine($"-> Insgesamt verbundene Zeit: {clientsCompleteTimeTotal.ToString(@"ddd\T\ hh\:mm\:ss")}");
            sb.AppendLine(string.Format(
                "-> Damit hätten wir {0} mal den 2. Weltkrieg führen können!",
                ((double)clientsCompleteTimeTotal.Ticks / (double)WW2DURATION.Ticks).ToString("0.000")));
            sb.AppendLine(string.Format(
                "-> Durchschnittlich verbundene Zeit: {0}",
                TimeSpan.FromTicks(clientsCompleteTimeTotal.Ticks / clients.Length).ToString(@"ddd\T\ hh\:mm\:ss")));
            sb.Append(string.Format(
                "-> Durchschnittlich aktiv Ratio: {0}",
                ((double)clientsActiveTimeTotal.Ticks / (double)clientsCompleteTimeTotal.Ticks).ToString("0.000")));


            Logger.Trace("Finished formatting channel description");
            return sb.ToString();
        }

        /// <summary>
        /// Formats the TopListChannelName with the topUser name
        /// </summary>
        /// <param name="topUser">Client object of the topUser</param>
        /// <returns></returns>
        private static string GetFormatedChannelName(User topUser)
        {
            Logger.Trace("Started formatting channel name");
            // Get the template name and fill in the Client.DisplayName
            var channelName = ConfigManager.Config.TopListChannelNameFormat.Replace("%NAME%", topUser.DisplayName);

            // ChannelName to long for TeamSpeak?
            if (channelName.Length > MAX_CHANNEL_NAME_LENGTH)
            {
                // Get max available username length for wildcard
                var maxNameLength = ConfigManager.Config.TopListChannelNameFormat.Length - "%NAME%".Length;
                var userName = topUser.DisplayName;

                // If topUser.DisplayName contains a '|' (pipe), cut it in front of the pipe
                // else just get the name trimed to the max available username length
                if (userName.Contains('|') && userName.IndexOf('|') <= maxNameLength)
                    userName = userName.Substring(0, userName.IndexOf('|')).Trim();
                else
                    userName = userName.Substring(0, maxNameLength).Trim();
                channelName = ConfigManager.Config.TopListChannelNameFormat.Replace("%NAME%", userName);
            }

            Logger.Trace("Finished formatting channel name");
            return channelName;
        }

        private async Task CollectClientTimes(DateTime lastRun)
        {
            Logger.Info("Collecting online time");

            bool anyChange = false;

            // Get all full clients
            Logger.Trace("Getting FullClients");
            var fullClients = await this.queryClient.GetFullClientsDetailedInfo();

            // Filter clients if they are connected in multiple client instances
            var clients = fullClients.DistinctBy(x => x.DatabaseId);

            // We want to pass all users here, because the function handles if the user is untracked etc.
            foreach (var ci in clients)
                anyChange |= UpdateUserTime(lastRun, ci);

            if (anyChange)
                UserManager.Save();

            Logger.Trace("Finished collecting online time");
        }

        /// <summary>
        /// Updates the user.ActiveTime and user.Total time if features are enabled
        /// and user will be tracked
        /// </summary>
        /// <param name="lastRun">The last runtime of time collecting</param>
        /// <param name="clientInfo">The detailed client info</param>
        /// <returns>If client got updated</returns>
        private bool UpdateUserTime(DateTime lastRun, GetClientDetailedInfo clientInfo)
        {
            // Check if User is in an ignored or not in a tracked group and ignore if true
            if (clientInfo.ServerGroupIds.Any(id =>
             ConfigManager.Config.TrackIgnoreUserGroups.Contains(id) ||
             !ConfigManager.Config.TrackUserGroups.Contains(id)))
            {
                return false;
            }

            bool update = false;
            var calculatedTime = (DateTime.Now - lastRun);

            var client = UserManager.GetUser(clientInfo);

            // Track total time
            if (ConfigManager.Config.TrackClientConnectedTimes)
            {
                client.TotalTime += calculatedTime;
                update = true;
            }

            // If we don't collect active time, we can return
            if (!ConfigManager.Config.TrackClientActiveTimes)
            {
                return update;
            }


            // Track active Time
            // TODO: Add detection if user is alone in channel, then stop collect active time

            // Check if user is in an ignored channel (afk, default connect channel)
            var conditionNotInIgnoredChannel = !ConfigManager.Config.TrackIgnoreChannels.Contains(clientInfo.ChannelId);


            // Ignore user if afk
            var conditionNotAway = !clientInfo.Away || ConfigManager.Config.TrackAFK;

            // Ignore user if muted
            var conditionNotMuted = !clientInfo.IsInputOrOutputMuted() || ConfigManager.Config.TrackOutputMuted;

            // Ignore user if idle is longer than threshold
            var conditionIdleTIme = clientInfo.IdleTime < ConfigManager.Config.TrackMaxIdleTime;

            if (conditionNotAway && conditionNotMuted &&
                conditionIdleTIme && conditionNotInIgnoredChannel)
            {
                client.ActiveTime += calculatedTime;
                update = true;
            }

            return update;
        }
    }
}

