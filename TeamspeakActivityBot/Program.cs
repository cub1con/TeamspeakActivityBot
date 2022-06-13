using Sentry;
using Sentry.Infrastructure;
using System;
using System.IO;
using TeamSpeak3QueryApi.Net;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Manager;

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

            // Load Config
            ConfigManager = new ConfigManager(CONFIG_FILE);

            // Initialise Sentry, then do the rest
            using (SentrySdk.Init(o =>
            {
                if (ConfigManager.Config.SentryDsn != "")
                {
                    o.Dsn = ConfigManager.Config.SentryDsn;
                }

                // Set traces_sample_rate to 1.0 to capture 100% of transactions for performance monitoring.
                // We recommend adjusting this value in production.
                o.TracesSampleRate = 1.0;
                o.ShutdownTimeout = TimeSpan.FromSeconds(5);
#if DEBUG
                o.Debug = true;
                o.Environment = "dev";
                o.DiagnosticLevel = SentryLevel.Debug;
                o.DiagnosticLogger = new TraceDiagnosticLogger(SentryLevel.Debug);
#else
                o.Environment = "prod";
#endif
            }))
            {
                // Check for valid config and options
                if (ConfigManager.ValidateConfig())
                    Environment.Exit(1); // Exit the Application

                try
                {
                    LogHelper.LogUpdate("[Press any key to exit]");
                    ClientManager = new ClientManager(CLIENTS_FILE);
                    var bot = new Bot(ClientManager, ConfigManager);
                    bot.Run().Wait();
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
                Console.WriteLine("Done.");

                return;
            }
        }


        static void DomainUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            LogHelper.LogError($"Runtime terminating: {args.IsTerminating}");
            Exception e = (Exception)args.ExceptionObject;
            HandleException(e);
        }

        static void HandleException(Exception ex)
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
    }
}
