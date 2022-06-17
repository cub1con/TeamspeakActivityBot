using System;

namespace TeamspeakActivityBot.Extensions
{
    public static class TimeSpanExtension
    {
        public static string GetAsDaysAndTime(this TimeSpan timeSpan)
        {
            return $"{timeSpan:ddd\\T\\ hh\\:mm\\:ss}";
        }
    }
}
