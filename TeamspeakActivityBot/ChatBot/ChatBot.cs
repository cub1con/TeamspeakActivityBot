using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.ChatBot
{
    public class ChatBot
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();

        private TeamSpeakClient queryClient;

#if DEBUG
        public char commandPrefix = '?';
#else
        public char commandPrefix = '!';
#endif

        public ChatBot(TeamSpeakClient query)
        {
            this.queryClient = query;
            // Here would be code to register all channels, but this
            // is not possible, because the queryClient has to be
            // in the channel to get the notification
            // await queryClient.RegisterTextChannelNotification();

            this.queryClient.Subscribe<TextMessage>(HandleServerChatMessages);
        }

        private async void HandleServerChatMessages(IReadOnlyCollection<TextMessage> textMessages)
        {
            foreach (var msg in textMessages)
            {
                // Prevent reacting to non command messages
                if (!msg.Message.StartsWith(commandPrefix))
                {
                    continue;
                }

                try
                {
                    // TODO: Add dynamic commands / adding text returning commands via command
                    // Example: !addNewCommand 'commandName' 'text the command will return'
                    // TODO: Make commands more dynamic

                    var command = GetCommandFromMessage(msg);
                    var message = string.Empty;

                    var cmd = ChatCommandList.Commands.FirstOrDefault(x => x.Name.Contains(command.Command));
                    if (cmd != null)
                    {
                        Logger.Info($"Starting {cmd.Name} - {msg.InvokerName}");
                        message = await cmd.HandleCommand(this.queryClient, msg.InvokerId, command);
                        Logger.Info($"Finished {command.Command} - {msg.InvokerName} -> {message}");
                    }
                    else
                    {
                        message = $"Command not found!";
                        Logger.Info(message);
                    }

                    await queryClient.SendMessage($"@{msg.InvokerName} - {message}", msg.TargetMode, msg.TargetClientId);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Command: '{msg.Message}'");
                    ExceptionHelper.HandleBotException(ex);
                }
            }
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
