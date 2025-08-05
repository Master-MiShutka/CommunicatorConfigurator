using System.Windows;
using Microsoft.Extensions.Logging;

namespace TMP.Work.CommunicatorPSDTU.UI.Wpf
{
    using Common.ViewModels;
    using TMP.Work.Common.Logger;
    using TMP.Work.CommunicatorPSDTU.Common;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IDisposable
    {
        const string LOGGER_NAME = "CommunicatorPSDTU";

        private ILoggerFactory? loggerFactory;

        static ILogger? logger;

        private MainViewModel? mainViewModel;

        private static readonly TextFileLoggerProvider LoggerProvider = new("journal.log") { MinLevel = LogLevel.Trace };

        static App()
        {
            logger = LoggerProvider.CreateLogger(LOGGER_NAME);

            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        public App()
        {
            this.DispatcherUnhandledException += this.App_DispatcherUnhandledException;

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

#if DEBUG

            System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(new System.Diagnostics.DefaultTraceListener());
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Warning;

            System.Windows.BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailure = System.Windows.BaseCompatibilityPreferences.HandleDispatcherRequestProcessingFailureOptions.Throw;

#endif

#pragma warning disable WPF0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            // this.ThemeMode = ThemeMode.System;
#pragma warning restore WPF0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        }

        private static void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            var trace = TMP.Shared.Common.Utils.GetExceptionDetails(e.Exception);

            string message = $"App_FirstChanceException :: {e.Exception.Message}\n" + trace;

            logger?.LogError(e.Exception, message);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string errorMsg = (e.ExceptionObject as Exception)?.Message ?? Common.Resources.Strings.ERROR_OCCURED;

            var trace = TMP.Shared.Common.Utils.GetExceptionDetails(e.ExceptionObject as Exception);

            string message = $"CurrentDomain_UnhandledException :: {errorMsg}\n" + trace;

            logger?.LogError(e.ExceptionObject as Exception, message);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var trace = TMP.Shared.Common.Utils.GetExceptionDetails(e.Exception);

            string message = $"App_DispatcherUnhandledException :: {e.Exception.Message}\n" + trace;

            logger?.LogError(e.Exception, message);

            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            LogLevel logLevel = LogLevel.Warning;

            if (e.Args.Length > 0)
            {
                bool isDebug = e.Args.Any(i => i.Contains("--debug"));

                if (isDebug)
                {
                    logLevel = LogLevel.Debug;
                }
                else
                {
                    bool isTrace = e.Args.Any(i => i.Contains("--trace"));
                    if (isTrace)
                    {
                        logLevel = LogLevel.Trace;
                    }
                    else
                    {
                        logLevel = LogLevel.Warning;
                    }
                }
            }

            LoggerProvider.MinLevel = logLevel;

            this.loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(LoggerProvider);
            });

            logger = this.loggerFactory.CreateLogger(LOGGER_NAME);

            logger.LogTrace("App OnStartup");

            logger.LogTrace("Log level: {Level}", logLevel);

            base.OnStartup(e);

            this.mainViewModel = new(logger);

            var mainWindow = new MainWindow()
            {
                DataContext = this.mainViewModel,
            };

            mainWindow.ShowDialog();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            logger?.LogTrace("App OnExit");

            if (this.mainViewModel != null)
            {
                try
                {
                    string json = System.Text.Json.JsonSerializer.Serialize<AppSettings>(this.mainViewModel.Settings, DefaultJsonSerializerOptions);

                    System.IO.File.WriteAllText(AppSettings.FileName, json, System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Не удалось сохранить настройки программы.");
                }
            }

            base.OnExit(e);
        }

        private static readonly System.Text.Json.JsonSerializerOptions DefaultJsonSerializerOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault | System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
        };

        public void Dispose()
        {
            LoggerProvider?.Dispose();

            this.mainViewModel?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
