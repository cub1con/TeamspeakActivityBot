using System;
using System.Linq;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamspeakActivityBot.Extensions;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Manager;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.BotHandler
{
    public class TextCommandHandler
    {
        public static async Task HandleMessage(TextMessage msg, TeamSpeakClient queryClient, ConfigManager configManager, UserManager userManager)
        {
            // TODO: Add dynamic commands / adding text returning commands via command
            // Example: !addNewCommand 'commandName' 'text the command will return'
            // TODO: Make commands more dynamic

            var command = GetCommandFromMessage(msg);

            LogHelper.LogUpdate($"Starting {command.Command} - {msg.InvokerName}");

            var message = string.Empty;

            // Interpret command
            switch (command.Command)
            {
                case "roll":
                    int maxRoll = 6;

                    // Check for argument, should be a number
                    if (command.Argument != null)
                    {
                        // TryParse the argument, if not valid, throw error
                        var parsed = int.TryParse(command.Argument, out maxRoll);
                        if (!parsed || maxRoll < 1) // Throw divided by zero exception
                        {
                            if (parsed && maxRoll == 0)
                            {
                                message = new DivideByZeroException().ToString();
                                break;
                            }

                            message = "No valid input!";
                            break;
                        }
                    }
                    // Roll a rundom number and report back to user
                    message = $"You rolled a {new Random().Next(1, maxRoll)}";
                    break;

                case "kick":
                    var userName = string.Empty;
                    var userId = -1;

                    // Check for argument
                    if (command.Argument != null)
                    {
                        var users = await queryClient.GetFilteredClients(configManager);
                        GetClientDetailedInfo user;

                        // random picks a random user and kicks him
                        if (command.Argument == "random" || command.Argument == "r")
                        {
                            var rnd = new Random().Next(1, users.Count);
                            user = users[users.Count - 1];
                        }
                        else
                        {
                            // Search for user to kick, report if not found
                            user = users.SingleOrDefault(x => x.NickName.ToLower().StartsWith(command.Argument));
                            if (user == null)
                            {
                                message = "User not found";
                                break;
                            }
                        }

                        userName = user.NickName;
                        userId = user.DatabaseId;
                    }
                    else
                    {
                        userName = msg.InvokerName;
                        userId = msg.InvokerId;
                    }

                    // Kick the user
                    await queryClient.KickClient(userId, KickOrigin.Server, "https://kek.com");
                    message = $"Kicked {userName} from the server";
                    break;

                case "rank":
                    // Get toplist ranking for user

                    if (!configManager.Config.TrackClientTimes)
                    {
                        message = "Client time tracking not enabled.";
                        break;
                    }

                    var clientId = await queryClient.GetUserByID(msg.InvokerId);                    

                    var rankUser = userManager.GetUserById(clientId.DatabaseId);

                    message = $"Your rank:\n"
                            + $"Active time: {userManager.Users.OrderByDescending(x => x.ActiveTime).ToList().IndexOf(rankUser) + 1} - {rankUser.ActiveTime.GetAsDaysAndTime()}\n"
                            + $"Total time: {userManager.Users.OrderByDescending(x => x.TotalTime).ToList().IndexOf(rankUser) + 1} - {rankUser.TotalTime.GetAsDaysAndTime()}";
                    break;

                case "meme":
                case "memes":
                    // Get some funky fresh memes
                    message = Misc.Memes.Captions[new Random().Next(0, Misc.Memes.Captions.Length - 1)];
                    break;
                case "hm":
                case "hmm":
                case "shrug":
                    message = @"¯\_(ツ)_/¯";
                    break;
                case "tf":
                case "tableflip":
                    message = @"(╯°□°）╯︵ ┻━┻";
                    break;
                case "uf":
                case "unflip":
                    message = @"┬─┬ ノ( ゜-゜ノ)";
                    break;
                case "help":
                    message = "Available Commands:\n"
                            + "!kick [random/r, Optional Username] - kicks you, a random, or specified user\n"
                            + "!meme(s) - Get some funky fresh memes!\n!help - You know.\n + "
                            + "!rank - Returns your current timerank (if time tracking is enabled)\n"
                            + "!roll [Optional number] - Rolls a dice with six sides [Rolls with x sides]\n"
                            + "!shrug, !tableflip, !unflip - shrug and flip!";
                    break;

                default:
                    message = $"Command not found!";
                    break;
            }

            LogHelper.LogUpdate($"Finished {command.Command} - {msg.InvokerName} -> {message}");
            await queryClient.SendGlobalMessage($"@{msg.InvokerName} - {message}");
        }

        private static TextCommand GetCommandFromMessage(TextMessage msg)
        {
            var command = new TextCommand();

            // returns -1 if there is no whitespace
            var whiteSpaceIndex = msg.Message.IndexOf(' ');

            if (whiteSpaceIndex == -1)
            {
                // Complete message is the command, remove !
                command.Command = msg.Message.Substring(1).ToLower();
            }
            else
            {
                // Cut the string, first substring will be command, second will be the argument
                command.Command = msg.Message.Substring(1, whiteSpaceIndex - 1).ToLower();

                // Advance whiteSpaceIndex + 1 to ignore the actual whitespace
                whiteSpaceIndex++;
                command.Argument = msg.Message.Substring(whiteSpaceIndex, msg.Message.Length - whiteSpaceIndex).ToLower();
            }

            return command;
        }
    }
}
