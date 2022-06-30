using System;
using Sentry;

namespace TeamspeakActivityBot.Helper
{
    public static class ExceptionHelper
    {
        public static void HandleException(Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            var logger = NLog.LogManager.GetLogger(memberName);
            int depth = 0;
            do
            {
                logger.Error(ex, $"Exception #{++depth}: {ex.Message}");
                logger.Error($"Stacktrace: {ex.StackTrace}");
                Console.WriteLine("===========================================");
                SentrySdk.CaptureException(ex);
            } while ((ex = ex.InnerException) != null);

        }

        public static void HandleBotException(Exception ex, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            var logger = NLog.LogManager.GetLogger(memberName);
            switch (ex)
            {
                case System.Net.Sockets.SocketException:
                    var socketEx = (System.Net.Sockets.SocketException)ex;
                    logger.Error(socketEx);
                    logger.Error($"Socket error: {socketEx.SocketErrorCode.ToString()}");
                    break;
                case TeamSpeak3QueryApi.Net.QueryException:
                    var querryEx = (TeamSpeak3QueryApi.Net.QueryException)ex;
                    logger.Error(querryEx);
                    if(querryEx.Error != null)
                    {
                        logger.Error(querryEx.Error.Message);
                        if (querryEx.Error.Message.ToLower() == ("invalid serverid"))
                        {
                            logger.Error("Check if instance is running or Setting 'QueryInstanceId' is set. Set to 0 to get default instance from server.");
                        }
                    }
                    break;
                default:
                    HandleException(ex);
                    break;
            }
        }
    }
}
