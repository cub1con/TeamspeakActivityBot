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

        public Bot(ClientManager cManager, ConfigManager cfgManager)
        {
            this.clientManager = cManager;
            this.configManager = cfgManager;
        }

        public async Task Run()
        {
            // Get all connected clients
            var bot = await GetConnectedClient();
            var botInfo = await bot.WhoAmI();

            var lastUserStatsUpdate = DateTime.Now;
            var lastChannelUpdate = DateTime.MinValue;


            // register server wide text notifications
            await bot.RegisterTextServerNotification();


            bot.Subscribe<TextMessage>(async data =>
            {
                foreach (var msg in data)
                {
                    // Prevent reacting to own messages
                    if (msg.InvokerId == botInfo.OriginServerId || !msg.Message.StartsWith('!'))
                    {
                        break;
                    }

                    var command = string.Empty;
                    var argument = string.Empty;

                    // returns -1 if there is no whitespace
                    var whiteSpaceIndex = msg.Message.IndexOf(' ');

                    if (whiteSpaceIndex == -1)
                    {
                        command = msg.Message.ToLower();
                    }
                    else
                    {
                        // Cut the string, first substing will be command, second will be the argument
                        command = msg.Message.Substring(0, whiteSpaceIndex).ToLower();
                        argument = msg.Message.Substring(whiteSpaceIndex, msg.Message.Length - whiteSpaceIndex).Trim().ToLower();
                    }


                    LogHelper.LogUpdate($"Starting {command} - {msg.InvokerName}");

                    // Interpret commands
                    var message = string.Empty;
                    switch (command)
                    {
                        case "!roll":
                            int maxRoll = 6;

                            // Check for argument, should be a number
                            if (argument != string.Empty)
                            {
                                // TryParse the argument, if not valid, throw error
                                var parsed = int.TryParse(argument, out maxRoll);
                                if (!parsed || maxRoll < 1) // Throw divided by zero exception
                                {
                                    if (maxRoll == 0)
                                    {
                                        message = new DivideByZeroException().ToString();
                                        break;
                                    }

                                    message = "No valid input!";
                                    break;
                                }
                            }
                            // Roll a rundom number and report back to user
                            var random = new Random().Next(1, maxRoll);
                            message = $"You rolled a {random}";
                            break;

                        case "!kick":
                            var userName = string.Empty;
                            var userId = -1;

                            // Check for argument
                            if (argument != string.Empty)
                            {
                                var users = await bot.GetClients();
                                var filterdUsers = users.Where(x => x.Type == ClientType.FullClient).ToArray();
                                GetClientInfo user;

                                // random picks a random user and kicks him
                                if (argument == "random" || argument == "r")
                                {
                                    var rnd = new Random().Next(1, filterdUsers.Length);
                                    user = filterdUsers[rnd - 1];
                                }
                                else
                                {
                                    // Search for user to kick, report if not found
                                    user = filterdUsers.SingleOrDefault(x => x.NickName.ToLower().StartsWith(argument));
                                    if (user == null)
                                    {
                                        message = "User not found";
                                        break;
                                    }
                                }

                                userName = user.NickName;
                                userId = user.Id;
                            }
                            else
                            {
                                userName = msg.InvokerName;
                                userId = msg.InvokerId;
                            }

                            // Kick the user
                            await bot.KickClient(userId, KickOrigin.Server, "Trololololololooololooooo.com");
                            message = $"Kicked {userName} from the server";
                            break;

                        case "!memes":
                        case "!meme":
                            // Get some funky fresh memes
                            var tmp = new Random().Next(0, Misc.Memes.Captions.Length - 1);
                            message = Misc.Memes.Captions[tmp];
                            break;
                        case "!help":
                            message = "Available Commands:\n!roll [Optional number]\n!kick ['random', Optional Username, yourself if no argument is provided]\n!meme(s) - Get some funky fresh memes!\n!help - You know.";
                            break;

                        default:
                            message = $"Command not found!";
                            break;
                    }

                    LogHelper.LogUpdate($"Finished {command} - {msg.InvokerName} -> {message}");
                    await bot.SendGlobalMessage($"@{msg.InvokerName} - {message}");
                }
            });

            while (!Console.KeyAvailable)
            {
                if (DateTime.Now - lastUserStatsUpdate >= configManager.Config.TimeLogInterval)
                {
                    await CollectOnlineTime(bot, lastUserStatsUpdate);
                    lastUserStatsUpdate = DateTime.Now;
                }
                if (DateTime.Now - lastChannelUpdate >= configManager.Config.ChannelUpdateInterval && configManager.Config.UpdateTopListChannel)
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

            // Default fresh installed Server Instance is 1, but we query it from the server because it might be not 1
            if (configManager.Config.InstanceId != 0)
            {
                await bot.UseServer(configManager.Config.InstanceId);
            }
            else
            {
                LogHelper.LogWarning("No Instance configured, fallback to query.");
                await bot.UseServer((await bot.GetServers()).FirstOrDefault().Id);
            }

            return bot;
        }

        private async Task SetTopList(TeamSpeakClient bot)
        {
            if (!clientManager.Clients.Data.Any())
            {
                LogHelper.LogWarning("Couldn't update channel info: no users!");
                return;
            }

            LogHelper.LogUpdate("Updating channel info");

            var clients = clientManager.Clients.Data.ToArray();
            var channelName = FormatChannelName(clients.OrderByDescending(x => x.ActiveTime).FirstOrDefault());

            // Set the channel description
            var description = FormatChannelDescription(clients);
            await bot.EditChannel(configManager.Config.TopListChannelId, ChannelEdit.channel_description, description);

            // Update channel name with mvp
            var channelInfo = await bot.GetChannelInfo(configManager.Config.TopListChannelId);
            if (channelInfo.Name != channelName)
                await bot.EditChannel(configManager.Config.TopListChannelId, ChannelEdit.channel_name, channelName);
        }

        private string FormatChannelDescription(Client[] clients)
        {
            // StringBuilder to hold the channel description
            var sb = new StringBuilder();

            // Format for TopUsers
            var topUsers = clients.OrderByDescending(x => x.ActiveTime).ToArray();

            var totalTimeTop = TimeSpan.FromTicks(topUsers.Sum(x => x.ActiveTime.Ticks));
            sb.AppendLine($"AKTIV:");
            sb.AppendLine($"Seit {configManager.Config.LoggingSince}:");
            sb.AppendLine(string.Join(Environment.NewLine, topUsers.Select(c => c.ToString()).ToArray()));
            sb.AppendLine("Fun facts:");
            sb.AppendLine(string.Format(
                "-> Insgesamt verschwendete Zeit: {0}",
                totalTimeTop.ToString(@"ddd\T\ hh\:mm\:ss")));
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
            sb.AppendLine($"Seit {configManager.Config.LoggingSince} verbunden:");
            sb.AppendLine(string.Join(Environment.NewLine, completeUsers.Select(c => c.ToConnectedTimeString()).ToArray()));
            sb.AppendLine("Fun facts:");
            sb.AppendLine(string.Format(
                "-> Insgesamt verbundene Zeit: {0}",
                totalTimeAll.ToString(@"ddd\T\ hh\:mm\:ss")));
            sb.AppendLine(string.Format(
                "-> Damit hätten wir {0} mal den 2. Weltkrieg führen können!",
                ((double)totalTimeAll.Ticks / (double)WW2DURATION.Ticks).ToString("0.000")));
            sb.Append(string.Format(
                "-> Durchschnittlich verbundene Zeit: {0}",
                TimeSpan.FromTicks(totalTimeAll.Ticks / completeUsers.Length).ToString(@"ddd\T\ hh\:mm\:ss")));

            return sb.ToString();
        }

        private string FormatChannelName(Client topUser)
        {
            var channelName = configManager.Config.TopListChannelNameFormat.Replace("%NAME%", topUser.DisplayName);
            if (channelName.Length > MAX_CHANNEL_NAME_LENGTH)
            {
                var maxNameLength = configManager.Config.TopListChannelNameFormat.Length - "%NAME%".Length;
                var userName = topUser.DisplayName;
                if (userName.Contains('|') && userName.IndexOf('|') <= maxNameLength)
                    userName = userName.Substring(0, userName.IndexOf('|')).Trim();
                else
                    userName = userName.Substring(0, maxNameLength).Trim();
                channelName = configManager.Config.TopListChannelNameFormat.Replace("%NAME%", userName);
            }
            return channelName;
        }

        private async Task CollectOnlineTime(TeamSpeakClient bot, DateTime lastRun)
        {
            LogHelper.LogUpdate("Collecting online time");
            var clients = await bot.GetClients();

            var clientInfos = new List<GetClientDetailedInfo>();
            foreach (var cl in clients.Where(c => c.Type == ClientType.FullClient))
                clientInfos.Add(await bot.GetClientInfo(cl.Id));

            var trackedClients = new List<GetClientDetailedInfo>();
            foreach (var cl in clientInfos
                .Where(c => !c.ServerGroupIds.Contains(this.configManager.Config.IgnoreUserGroup) && // Ignore User if in specified group
                !configManager.Config.IgnoreChannels.Contains(c.ChannelId) &&                        // Ignore User if in specified channels
                c.ServerGroupIds.Any(id => configManager.Config.UserGroups
                .Contains(id)))) trackedClients
                 .Add(cl);

            bool anyChange = false;

            foreach (var ci in trackedClients) anyChange |= UpdateClientTime(lastRun, ci);
            if (anyChange)
                clientManager.Clients.Save();
        }

        private bool UpdateClientTime(DateTime lastRun, GetClientDetailedInfo clientInfo)
        {
            // Will always get set to true, while TrackTotalTime is mandatory
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
            client.ConnectedTime += calculatedTime;
            update = true;

            // Track active Time
            // Ignore user if afk
            var conditionNotAway = !clientInfo.Away && !configManager.Config.LogAFK;

            // Ignore user if muted
            var conditionNotMuted = !clientInfo.IsInputOrOutputMuted() && !configManager.Config.LogOutputMuted;

            // Ignore user if idle is longer than threshold
            var conditionIdleTIme = clientInfo.IdleTime < configManager.Config.MaxIdleTime;

            if (conditionNotAway && conditionNotMuted && conditionIdleTIme)
            {
                client.ActiveTime += calculatedTime;
                update = true;
            }

            return update;
        }
    }
}

