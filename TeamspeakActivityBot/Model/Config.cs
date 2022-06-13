using Newtonsoft.Json;
using System;

namespace TeamspeakActivityBot.Model
{
    public class Config
    {
        public string Host { get; set; }
        public int HostPort { get; set; }
        public string QueryUsername { get; set; }
        public string QueryPassword { get; set; }
        public int QueryInstanceId { get; set; }


        // Client time tracking
        [JsonIgnore]
        public bool TrackClientTimes { get => TrackClientActiveTimes || TrackClientConnectedTimes; }
        public bool TrackClientActiveTimes { get; set; }
        public bool TrackClientConnectedTimes { get; set; }
        public TimeSpan TrackTimeLogInterval { get; set; }
        public bool TrackAFK { get; set; }
        public bool TrackOutputMuted { get; set; }
        public TimeSpan TrackMaxIdleTime { get; set; }
        public int[] TrackUserGroups { get; set; }
        public int[] TrackIgnoreUserGroups { get; set; }
        public int[] TrackIgnoreChannels { get; set; }
        public DateTime TrackLoggingSince { get; set; }

        // Top list
        public bool TopListUpdateChannel { get; set; }
        public int TopListChannelId { get; set; }
        // Wildcard for username: %NAME%
        public string TopListChannelNameFormat { get; set; }
        public TimeSpan TopListChannelUpdateInterval { get; set; }

        // Chat commands
        public bool EnableChatCommands { get; set; }

        // Just for dev
        public string SentryDsn { get; set; }

        public Config()
        {
            Host = "localhost";
            HostPort = 10011;
            QueryUsername = "";
            QueryPassword = "";
            QueryInstanceId = 0;

            // Time collection
            TrackClientActiveTimes = true;
            TrackClientConnectedTimes = true;
            TrackLoggingSince = DateTime.Now;
            TrackTimeLogInterval = TimeSpan.FromSeconds(10);
            TrackMaxIdleTime = TimeSpan.FromSeconds(30);
            TrackAFK = false;
            TrackOutputMuted = false;
            TrackUserGroups = new int[] { 9, 10, 12 };
            TrackIgnoreUserGroups = new int[] { 13, };
            TrackIgnoreChannels = new int[] { 19, 18, 17, 16, 15, 14 };

            // Chat commands
            EnableChatCommands = false;

            // TopListChannel
            TopListUpdateChannel = true;
            TopListChannelId = -1;
            TopListChannelNameFormat = "[cspacer9]|| MVP: %NAME% ||";
            TopListChannelUpdateInterval = TimeSpan.FromSeconds(30);

            // Dev
            SentryDsn = "";
        }
    }
}
