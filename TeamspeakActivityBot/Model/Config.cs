using System;

namespace TeamspeakActivityBot.Model
{
    public class Config
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string QueryUsername { get; set; }
        public string QueryPassword { get; set; }
        public int InstanceId { get; set; }

        public TimeSpan TimeLogInterval { get; set; }
        public TimeSpan MaxIdleTime { get; set; }
        public DateTime LoggingSince { get; set; }

        public bool LogAFK { get; set; }
        public bool LogOutputMuted { get; set; }

        public int[] UserGroups { get; set; }
        public int IgnoreUserGroup { get; set; }
        public int[] IgnoreChannels { get; set; }


        public bool UpdateTopListChannel { get; set; }
        public int TopListChannelId { get; set; }
        // Wildcard for username: %NAME%
        public string TopListChannelNameFormat { get; set; }
        public TimeSpan ChannelUpdateInterval { get; set; }


        // Just for dev
        public string SentryDsn { get; set; }

        public Config()
        {
            Host = "localhost";
            Port = 10011;
            QueryUsername = "";
            QueryPassword = "";
            InstanceId = 0;
            LoggingSince = DateTime.Now;
            TimeLogInterval = TimeSpan.FromSeconds(10);
            MaxIdleTime = TimeSpan.FromSeconds(30);
            LogAFK = false;
            LogOutputMuted = false;
            UserGroups = new int[] { 9, 10, 12 };
            IgnoreUserGroup = 13;
            IgnoreChannels = new int[] { 19, 18, 17, 16, 15, 14 };

            // TopListChannel
            UpdateTopListChannel = true;
            TopListChannelId = -1;
            TopListChannelNameFormat = "[cspacer9]|| MVP: %NAME% ||";
            ChannelUpdateInterval = TimeSpan.FromSeconds(30);

            SentryDsn = "";
        }
    }
}
