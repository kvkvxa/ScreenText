using System.Windows;
using WpfApplication = System.Windows.Application;

namespace ScreenText;

public partial class App : WpfApplication
{
    private ApplicationHost? _host;

    public App()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        ScreenText.Platform.NativeMethods.SetProcessDpiAwarenessContext(ScreenText.Platform.NativeMethods.PerMonitorAwareV2);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _host = new ApplicationHost();
        if (!_host.Start())
        {
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
