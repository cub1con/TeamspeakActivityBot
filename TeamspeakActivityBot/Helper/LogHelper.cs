using System;

namespace TeamspeakActivityBot.Helper
{
    public static class LogHelper
    {
        public static void LogUpdate(string message)
        {
            Log($"[>] {message}");
        }

        public static void LogWarning(string message)
        {
            Log($"[!] {message}");
        }

        public static void LogError(string message)
        {
            Log($"[X] {message}");
        }

        private static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now:dd.MM.yy hh:mm:ss} | {message}");
        }
    }
}
