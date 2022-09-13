using NLog;
using Sentry;
using Sentry.Infrastructure;
using System;
using System.Reflection;
using TeamspeakActivityBot.Helper;
using TeamspeakActivityBot.Manager;

namespace TeamspeakActivityBot
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(DomainUnhandledExceptionHandler);

            string environment = "prod";
#if DEBUG
            environment = "dev";
            Logger.Info("Running in debug mode");
#endif

            var assembly = Assembly.GetExecutingAssembly().GetName();
            var release = $"{assembly.Name} - {environment}-{assembly.Version}";

            // "Draw" TAB logo
            Console.WriteLine(Misc.Memes.Logo);
            // Print version
            Logger.Info($"{release} says hi");


            Logger.Info("Loading config");
            ConfigManager.Load();
            // Check for valid config and options
            if (!ConfigManager.ValidateConfig())
                Environment.Exit(0); // Exit the Application

            Logger.Info("Loading clients");
            UserManager.Load();

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
                o.DiagnosticLevel = SentryLevel.Debug;
                o.DiagnosticLogger = new TraceDiagnosticLogger(SentryLevel.Debug);
#endif
                o.Debug = true;
                o.Environment = environment;
                o.Release = release;
            }))
            {
                try
                {
                    var bot = new Bot();
                    bot.Run().Wait();
                    bot.Dispose();
                    Logger.Info("Done.");
                }
                catch (Exception ex)
                {
                    ExceptionHelper.HandleException(ex);
                    Logger.Error("Terminating...");
                }

                return;
            }
        }


        static void DomainUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logger.Error($"Unhandled exception! - Runtime terminating: {args.IsTerminating}");
            Exception e = (Exception)args.ExceptionObject;
            ExceptionHelper.HandleException(e);
        }
    }
}
