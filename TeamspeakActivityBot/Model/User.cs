using System;

namespace TeamspeakActivityBot.Model
{
    public class User
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public TimeSpan ActiveTime { get; set; }
        public TimeSpan TotalTime { get; set; }
    }
}
