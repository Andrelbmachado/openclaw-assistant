using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace OpenClaw.Desktop;

public partial class MainWindow : Window
{
    private readonly GatewayManager _gateway;
    private bool _webMessageHandlerAttached;

    public MainWindow(GatewayManager gateway, GatewayStartupResult startup)
    {
        InitializeComponent();
        _gateway = gateway;
        Loaded += async (_, _) => await ApplyGatewayStateAsync(startup);
        Closed += (_, _) => _gateway.Dispose();
    }

    private async Task ApplyGatewayStateAsync(GatewayStartupResult startup)
    {
        if (!startup.IsAvailable)
        {
            SetUnavailable(startup.Detail);
            return;
        }

        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenClaw",
                "DesktopWebView2");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await Browser.EnsureCoreWebView2Async(environment);
            Browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            Browser.CoreWebView2.Settings.IsWebMessageEnabled = true;
            if (!_webMessageHandlerAttached)
            {
                Browser.CoreWebView2.WebMessageReceived += HandleWebMessage;
                _webMessageHandlerAttached = true;
            }
            Browser.CoreWebView2.NewWindowRequested += (_, eventArgs) =>
            {
                eventArgs.Handled = true;
                Process.Start(new ProcessStartInfo(eventArgs.Uri) { UseShellExecute = true });
            };
            Browser.Source = _gateway.ResolveDashboardUri();
            Browser.Visibility = Visibility.Visible;
            UnavailablePanel.Visibility = Visibility.Collapsed;
            SetStatus("Connected", "#22C55E");
        }
        catch (Exception error)
        {
            SetUnavailable($"The desktop window could not initialize WebView2: {error.Message}");
        }
    }

    private void HandleWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs eventArgs)
    {
        try
        {
            if (!Uri.TryCreate(eventArgs.Source, UriKind.Absolute, out var source) || !source.IsLoopback)
            {
                return;
            }

            using var message = JsonDocument.Parse(eventArgs.WebMessageAsJson);
            var root = message.RootElement;
            if (root.GetProperty("type").GetString() != "openclaw.auth")
            {
                return;
            }

            var provider = root.GetProperty("provider").GetString();
            var method = root.GetProperty("method").GetString();
            var command = (provider, method) switch
            {
                ("openai", "api-key") => "openclaw models auth login --provider openai --method api-key --set-default",
                ("openai", "oauth") => "openclaw models auth login --provider openai --method oauth --set-default",
                ("anthropic", "api-key") => "openclaw models auth login --provider anthropic --method api-key --set-default",
                ("anthropic", "cli") => "openclaw models auth login --provider anthropic --method cli --set-default",
                ("google", "api-key") => "openclaw models auth login --provider google --method api-key --set-default",
                ("google-gemini-cli", "oauth") => "openclaw models auth login --provider google-gemini-cli --method oauth --set-default",
                _ => null,
            };
            if (command is null)
            {
                return;
            }

            var terminal = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            terminal.ArgumentList.Add("-NoLogo");
            terminal.ArgumentList.Add("-NoExit");
            terminal.ArgumentList.Add("-Command");
            terminal.ArgumentList.Add(command);
            Process.Start(terminal);
        }
        catch (Exception error)
        {
            MessageBox.Show(
                this,
                $"Could not start provider authentication: {error.Message}",
                "OpenClaw Desktop",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SetUnavailable(string detail)
    {
        Browser.Visibility = Visibility.Collapsed;
        UnavailablePanel.Visibility = Visibility.Visible;
        UnavailableDetail.Text = detail + "\n\nWeb version: http://127.0.0.1:18789/";
        SetStatus("Gateway offline", "#EF4444");
    }

    private void SetStatus(string text, string color)
    {
        StatusText.Text = text;
        StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private async void TryAgainButton_Click(object sender, RoutedEventArgs e)
    {
        SetStatus("Starting Gateway", "#F59E0B");
        UnavailableDetail.Text = "Trying to start the local Gateway...";
        await ApplyGatewayStateAsync(await _gateway.EnsureRunningAsync());
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        Browser.CoreWebView2?.Reload();
    }

    private void BrowserButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(_gateway.ResolveDashboardUri().ToString()) { UseShellExecute = true });
    }
}
