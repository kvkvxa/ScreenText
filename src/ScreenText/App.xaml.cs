using System.Windows;
using System.Threading;
using System.IO;
using WpfApplication = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ScreenText;

public partial class App : WpfApplication
{
    private ApplicationHost? _host;
    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScreenText",
        "crash.log");

    public App()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        ScreenText.Platform.NativeMethods.SetProcessDpiAwarenessContext(ScreenText.Platform.NativeMethods.PerMonitorAwareV2);
        
        // Глобальная обработка необработанных исключений
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            _host = new ApplicationHost();
            if (!_host.Start())
            {
                Shutdown();
            }
        }
        catch (Exception exception)
        {
            HandleCriticalException(exception, "при запуске приложения");
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _host?.Dispose();
        }
        catch (Exception exception)
        {
            // Логируем ошибки при завершении, но не прерываем выход
            LogException(exception, "при завершении работы");
        }
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        HandleCriticalException(e.Exception, "в интерфейсе пользователя");
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception ?? new Exception("Неизвестная ошибка домена");
        HandleCriticalException(exception, "в домене приложения");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        LogException(e.Exception, "в фоновой задаче");
    }

    private void HandleCriticalException(Exception exception, string context)
    {
        LogException(exception, context);
        
        try
        {
            var message = $"Произошла критическая ошибка {context}.\n\n" +
                         $"Приложение будет закрыто.\n" +
                         $"Файл журнала: {LogFilePath}\n\n" +
                         $"Детали: {exception.GetType().Name}: {exception.Message}";
            
            MessageBox.Show(message, "ScreenText - Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
            // Не можем показать UI, просто логируем
        }
    }

    private static void LogException(Exception exception, string context)
    {
        try
        {
            var logDirectory = Path.GetDirectoryName(LogFilePath)!;
            Directory.CreateDirectory(logDirectory);
            
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] CRITICAL ERROR {context}:\n" +
                          $"Type: {exception.GetType().FullName}\n" +
                          $"Message: {exception.Message}\n" +
                          $"StackTrace:\n{exception.StackTrace}\n";
            
            if (exception.InnerException != null)
            {
                logEntry += $"\nInner Exception:\n{exception.InnerException}\n";
            }
            
            logEntry += new string('-', 80) + "\n\n";
            
            File.AppendAllText(LogFilePath, logEntry);
        }
        catch
        {
            // Не можем записать лог - ничего не поделаешь
        }
    }
}
