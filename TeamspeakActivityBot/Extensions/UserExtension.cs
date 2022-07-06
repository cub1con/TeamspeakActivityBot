using System;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot.Extensions
{
    public static class UserExtension
    {
        public static string GetActiveTimeAndName(this User cl)
        {
            return $"{cl.ActiveTime.GetAsDaysAndTime()} - {cl.DisplayName}";
        }

        public static string GetTotalTimeAndName(this User cl) 
        {
            return $"{cl.TotalTime.GetAsDaysAndTime()} - {cl.DisplayName} - {cl.GetActiveTimeRatio()}";
        }

        public static string GetActiveTimeRatio(this User cl)
        {
            return ((double)cl.ActiveTime.Ticks / (double)cl.TotalTime.Ticks).ToString("0.000");
        }
    }
}
