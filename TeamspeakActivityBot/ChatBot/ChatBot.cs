using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Chat
{
    public static class ChatBot
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();

#if DEBUG
        public static char commandPrefix = '?';
#else
        public static char commandPrefix = '!';
#endif

        public static async void HandleServerChatMessages(IReadOnlyCollection<TextMessage> textMessages, TeamSpeakClient queryClient)
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

                    var command = GetTextCommandFromString(msg.Message);
                    var message = string.Empty;

                    var cmd = ChatCommandList.Commands.FirstOrDefault(x => x.Name.Contains(command.Command));
                    if (cmd != null)
                    {
                        Logger.Info($"Starting {nameof(cmd)}: {command.Command} - {msg.InvokerName}");
                        message = await cmd.HandleCommand(queryClient, msg.InvokerId, command);
                        Logger.Info($"Finished {nameof(cmd)}: {command.Command} - {msg.InvokerName} -> {message}");
                    }
                    else
                    {
                        message = $"Command '{command.Command}' not found!";
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

        private static TextCommand GetTextCommandFromString(string msg)
        {
            // returns -1 if there is no whitespace
            var whiteSpaceIndex = msg.IndexOf(' ');

            if (whiteSpaceIndex == -1)
            {
                // Complete message is the command, remove !
                return new TextCommand() { Command = msg.Substring(1).ToLower() };
            }

            // Cut the string, first substring will be command, second will be the argument
            return new TextCommand()
            {
                Command = msg.Substring(1, whiteSpaceIndex - 1).ToLower(),
                // whiteSpaceIndex + 1 to ignore the actual whitespace
                Argument = msg.Substring(whiteSpaceIndex, msg.Length - (whiteSpaceIndex + 1)).ToLower()
            };
        }
    }
}
