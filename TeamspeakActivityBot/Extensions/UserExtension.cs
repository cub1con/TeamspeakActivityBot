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
            return $"{cl.TotalTime.GetAsDaysAndTime()} - {cl.DisplayName}";
        }
    }
}
