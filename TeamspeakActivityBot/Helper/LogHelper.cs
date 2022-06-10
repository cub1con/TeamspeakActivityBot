using System;

namespace TeamspeakActivityBot.Helper
{
    public static class LogHelper
    {
        public static void LogUpdate(string message)
        {
            Console.WriteLine($"[>] {message}");
        }

        public static void LogWarning(string message)
        {
            Console.WriteLine($"[!] {message}");
        }

        public static void LogError(string message)
        {
            Console.WriteLine($"[X] {message}");
        }
    }
}
