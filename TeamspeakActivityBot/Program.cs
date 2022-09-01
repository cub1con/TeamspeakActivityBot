using NLog;
using Sentry;
using Sentry.Infrastructure;
using System;
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


            // "Draw" TAB logo
            Console.WriteLine(Misc.Memes.Logo);
            // Print version
            Logger.Info($"TeamspeakActivityBot says hi - v.{typeof(Program).Assembly.GetName().Version}");
#if DEBUG
            Logger.Info("Running in debug mode");
#endif


            Logger.Info("Loading config");
            ConfigManager.Load();
            // Check for valid config and options
            if (!ConfigManager.ValidateConfig())
                Environment.Exit(1); // Exit the Application

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
                o.Debug = true;
                o.Environment = "dev";
                o.DiagnosticLevel = SentryLevel.Debug;
                o.DiagnosticLogger = new TraceDiagnosticLogger(SentryLevel.Debug);
#else
                o.Environment = "prod";
#endif
            }))
            {
                try
                {
                    var bot = new Bot();
                    bot.Run().Wait();
                    Logger.Info("Done.");
                }
                catch (Exception ex)
                {
                    ExceptionHelper.HandleException(ex);
                    Logger.Error("Terminating...");
                    Environment.Exit(1);
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
