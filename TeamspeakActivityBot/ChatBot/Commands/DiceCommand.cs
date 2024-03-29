﻿using System;
using System.Threading.Tasks;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamspeakActivityBot.Chat.Commands.Abstraction;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Chat.Commands
{
    public class DiceCommand : IChatCommand
    {
        public string[] Name => new string[] { "dice", "roll" };

        public string HelpDescription => "[Optional number] - rolls a dice with six sides [Rolls with x sides]";

        public async Task<string> HandleCommand(TeamSpeakClient queryClient, int invokerId, TextCommand command)
        {
            int maxRoll = 7; // 7 because random will roll BETWEEN not including

            // Check for argument, should be a number
            if (command.Argument != null)
            {
                // TryParse the argument, if not valid, throw error
                var parsed = int.TryParse(command.Argument, out maxRoll);
                if (!parsed || maxRoll < 1) // Throw divided by zero exception
                {
                    if (parsed && maxRoll == 0)
                    {
                        return new DivideByZeroException().ToString();
                    }

                    return "No valid input!";
                }
            }

            // Roll a rundom number and report back to user
            return $"You rolled a {new Random().Next(1, maxRoll)}";
        }
    }
}
