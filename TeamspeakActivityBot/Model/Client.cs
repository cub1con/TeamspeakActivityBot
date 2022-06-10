using System;

namespace TeamspeakActivityBot.Model
{
    public class Client
    {
        public int ClientId { get; set; }
        public string DisplayName { get; set; }
        public TimeSpan ActiveTime { get; set; }
        public TimeSpan ConnectedTime { get; set; }

        public override string ToString()
        {
            return $"{ActiveTime:ddd\\T\\ hh\\:mm\\:ss} - {DisplayName}";
        }


        public string ToActiveTimeString()
        {
            return this.ToString();
        }

        public string ToConnectedTimeString()
        {
            return $"{ConnectedTime:ddd\\T\\ hh\\:mm\\:ss} - {DisplayName}";
        }
    }
}
