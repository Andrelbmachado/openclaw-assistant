using System.Windows;

namespace OpenClaw.Desktop;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var splash = new SplashWindow();
        splash.Show();

        var gateway = new GatewayManager();
        var startup = gateway.EnsureRunningAsync();
        var minimumSplash = Task.Delay(TimeSpan.FromSeconds(2.2));
        await Task.WhenAll(startup, minimumSplash);

        var main = new MainWindow(gateway, startup.Result);
        MainWindow = main;
        main.Show();
        splash.Close();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }
}
