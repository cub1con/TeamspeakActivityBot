using System;

namespace TeamspeakActivityBot.Model
{
    public class Client
    {
        public string ClientId { get; set; }
        public string DisplayName { get; set; }
        public TimeSpan ActiveTime { get; set; }

        public override string ToString()
        {
            return $"{ActiveTime.ToString(@"ddd\T\ hh\:mm\:ss")} - {DisplayName}";
        }
    }
}
