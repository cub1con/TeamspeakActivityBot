using TeamSpeak3QueryApi.Net.Specialized.Responses;

namespace TeamspeakActivityBot.Extensions
{
    public static class ClientExtension
    {
        public static bool IsInputOrOutputMuted(this GetClientDetailedInfo clientInfo)
        {
            return clientInfo.OutputMuted || clientInfo.OutputOnlyMuted || clientInfo.InputMuted;
        }
    }
}
