using NLog;
using Sentry;
using System;

namespace TeamspeakActivityBot.Helper
{
    public static class ExceptionHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void HandleException(Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            int depth = 0;
            do
            {
                Logger.Error($"{memberName}");
                Logger.Error(ex, $"Exception #{++depth}: {ex.Message}");
                Logger.Error($"Stacktrace: {ex.StackTrace}");
                Console.WriteLine("===========================================");
                SentrySdk.CaptureException(ex);
            } while ((ex = ex.InnerException) != null);

        }

        public static void HandleBotException(Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            switch (ex)
            {
                case System.Net.Sockets.SocketException:
                    var socketEx = (System.Net.Sockets.SocketException)ex;
                    Logger.Error(socketEx);
                    Logger.Error($"Socket error: {socketEx.SocketErrorCode}");
                    break;
                case TeamSpeak3QueryApi.Net.QueryException:
                    var querryEx = (TeamSpeak3QueryApi.Net.QueryException)ex;
                    Logger.Error(querryEx);
                    if (querryEx.Error != null)
                    {
                        Logger.Error(querryEx.Error.Message);
                        if (querryEx.Error.Message.ToLower() == ("invalid serverid"))
                        {
                            Logger.Error("Check if instance is running or setting 'QueryInstanceId' is set to the correct instance. Set to 0 to get default instance from server.");
                        }
                    }
                    break;
                default:
                    HandleException(ex, memberName);
                    break;
            }
        }
    }
}
