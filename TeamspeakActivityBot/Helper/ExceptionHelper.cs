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
                    if (querryEx.Error != null)
                    {
                        switch (querryEx.Error.Message.ToLower())
                        {
                            case "server is not running":
                            case "invalid serverid":
                                Logger.Error("Instance not found! -  Hint: Check if instance is running or the setting 'QueryInstanceId' is set to the correct instance. Set to 0 to get default instance from server.");
                                break;


                            case "nickname is already in use":
                                Logger.Error("Nickkname is already in use! - Hint: Check if bot is still connected or awaiting timeout.");
                                break;

                            default:
                                Logger.Error(querryEx);
                                Logger.Error(querryEx.Error.Message);
                                break;
                        }
                    }
                    else
                    {
                        Logger.Error(querryEx);
                    }
                    break;
                default:
                    HandleException(ex, memberName);
                    break;
            }
        }
    }
}
