using Sentry;
using System;
using System.IO;
using TeamSpeak3QueryApi.Net;
using TeamspeakActivityBot.Manager;
using TeamspeakActivityBot.Model;

namespace TeamspeakActivityBot
{
    class Program
    {
        private static FileInfo CLIENTS_FILE = new FileInfo(Path.Combine(Environment.CurrentDirectory, "clients.json"));
        private static FileInfo CONFIG_FILE = new FileInfo(Path.Combine(Environment.CurrentDirectory, "config.json"));

        private static ClientManager ClientManager;
        private static ConfigManager ConfigManager;

        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(DomainUnhandledExceptionHandler);

#if DEBUG
            while (!System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("Waiting for Debugger...");
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine("Debugger attached!");
#endif

            ClientManager = new ClientManager(CLIENTS_FILE);
            ConfigManager = new ConfigManager(CONFIG_FILE);
            using (SentrySdk.Init(o =>
            {
                o.Dsn = ConfigManager.Config.SentryDsn;
                // When configuring for the first time, to see what the SDK is doing:
                o.Debug = true;
                o.ShutdownTimeout = TimeSpan.FromSeconds(5);
                // Set traces_sample_rate to 1.0 to capture 100% of transactions for performance monitoring.
                // We recommend adjusting this value in production.
#if DEBUG
                o.Environment = "dev";
#else
                o.Environment = "prod";

                o.TracesSampleRate = 1.0;
#endif
            }))
            {
                try
                {
                    Console.WriteLine("[Press any key to exit]");
                    var bot = new Bot(ClientManager, ConfigManager);
                    bot.Run().Wait();
                }
                catch (Exception ex)
                {
                    int depth = 0;
                    do
                    {
                        Console.WriteLine("Exception #{0}: {1}", ++depth, ex.Message);
                        if (ex.GetType() == typeof(QueryException))
                            Console.WriteLine("Error: {0}", ((QueryException)ex).Error.Message);
                        Console.WriteLine("Stacktrace: {0}", ex.StackTrace);
                        Console.WriteLine("===========================================");
                        SentrySdk.CaptureException(ex);
                    } while ((ex = ex.InnerException) != null);
                }
                Console.WriteLine("Done.");
            }
        }


        static void DomainUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("MyHandler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
            SentrySdk.CaptureException(e);
        }
    }
}
