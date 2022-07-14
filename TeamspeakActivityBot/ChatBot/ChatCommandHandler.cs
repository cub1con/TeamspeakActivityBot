using System.Collections.Generic;
using TeamspeakActivityBot.ChatBot.Commands;
using TeamspeakActivityBot.ChatBot.Commands.Abstraction;

namespace TeamspeakActivityBot.ChatBot
{
    public static class ChatCommandList
    {
        // TODO: test performance
        //private static Dictionary<string, ChatCommand> commands;

        //public static Dictionary<string, ChatCommand> Commands => commands ?? (commands = LoadCommands());
        //private static Dictionary<string, ChatCommand> LoadCommands()
        //{
        //    return new Dictionary<string, ChatCommand>
        //    {
        //        { "roll", new DiceCommand() },
        //        { "kick", new KickCommand() },
        //        { "help", new HelpCommand() },
        //        { "rank", new RankCommand() },
        //        { "config", new ConfigCommand() },
        //        { "memes", new MemesCommand() }
        //    };
        //}


        private static List<IChatCommand> commands;

        public static IEnumerable<IChatCommand> Commands => commands ?? (commands = LoadCommands());
        private static List<IChatCommand> LoadCommands()
        {
            return new List<IChatCommand>()
            {
                new DiceCommand(),
                new KickCommand(),
                new HelpCommand(),
                new RankCommand(),
                new ConfigCommand(),
                new MemesCommand()
            };
        }



        //                case "shrug":
        //                    message = @"¯\_(ツ)_/¯";
        //                    break;

        //                case "tf":
        //                case "tableflip":
        //                    message = @"(╯°□°）╯︵ ┻━┻";
        //                    break;

        //                case "uf":
        //                case "unflip":
        //                    message = @"┬─┬ ノ( ゜-゜ノ)";
        //                    break;

        //                case "continue": // Easteregg for LW
        //                    message = "Stepcode i'm stuck!";
        //                    break;
        //#if DEBUG
        //                // Dedicated command to test crashing
        //                case "crashme":
        //                    throw new Exception("crashme Command!");
        //#endif
    }
}
