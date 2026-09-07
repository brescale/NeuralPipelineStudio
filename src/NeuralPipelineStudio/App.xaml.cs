using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace NeuralPipelineStudio
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Dispatcher thread exceptions (UI thread)
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // 2. Non-UI / background thread exceptions
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // 3. Unobserved Task exceptions
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash("DispatcherUnhandledException", e.Exception);
            e.Handled = true; // Prevent abrupt app death
            MessageBox.Show($"[Neural Pipeline Studio - Guardrail Notice]\nAn unexpected UI event was handled safely:\n\n{e.Exception.Message}\n\nDetails logged to studio_crash.log",
                            "Studio Safety Guardrail", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash("AppDomainUnhandledException", ex);
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash("UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        private static void LogCrash(string source, Exception ex)
        {
            try
            {
                string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "studio_crash.log");
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{source}]\n{ex}\n--------------------------------------------------\n";
                File.AppendAllText(logFile, entry);
            }
            catch { }
        }
    }
}
