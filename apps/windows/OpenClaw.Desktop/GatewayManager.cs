using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace OpenClaw.Desktop;

public sealed record GatewayStartupResult(bool IsAvailable, bool WasStarted, string Detail);

public sealed class GatewayManager : IDisposable
{
    public static readonly Uri DashboardUri = new("http://127.0.0.1:18789/");

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(2) };
    private Process? _gatewayProcess;

    public async Task<GatewayStartupResult> EnsureRunningAsync()
    {
        if (await IsAvailableAsync())
        {
            return new(true, false, "Connected to the local Gateway.");
        }

        var launch = ResolveLaunchCommand();
        if (launch is null)
        {
            return new(false, false, "OpenClaw Gateway was not found. Install the OpenClaw CLI or start the Gateway manually.");
        }

        try
        {
            _gatewayProcess = Process.Start(launch.Value.StartInfo);
        }
        catch (Exception error)
        {
            return new(false, false, $"Could not start the Gateway: {error.Message}");
        }

        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (await IsAvailableAsync())
            {
                return new(true, true, launch.Value.Description);
            }

            if (_gatewayProcess?.HasExited == true)
            {
                break;
            }

            await Task.Delay(500);
        }

        return new(false, true, "The Gateway process started, but the dashboard did not become available.");
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            using var response = await _http.GetAsync(DashboardUri);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Uri ResolveDashboardUri()
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".openclaw",
            "openclaw.json");
        if (!File.Exists(configPath))
        {
            return DashboardUri;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            var gateway = document.RootElement.GetProperty("gateway");
            var auth = gateway.GetProperty("auth");
            if (auth.TryGetProperty("token", out var tokenElement))
            {
                var token = tokenElement.GetString();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    return new Uri($"{DashboardUri}#token={Uri.EscapeDataString(token)}");
                }
            }
        }
        catch
        {
            // The Control UI can still present its normal login screen.
        }

        return DashboardUri;
    }

    private static (ProcessStartInfo StartInfo, string Description)? ResolveLaunchCommand()
    {
        var configuredCli = Environment.GetEnvironmentVariable("OPENCLAW_CLI_PATH");
        if (!string.IsNullOrWhiteSpace(configuredCli) && File.Exists(configuredCli))
        {
            return CreateLaunch(configuredCli, "gateway run", Path.GetDirectoryName(configuredCli), "Started the configured OpenClaw Gateway.");
        }

        var installedCli = FindInstalledCli();
        if (installedCli is not null)
        {
            return CreateLaunch(installedCli, "gateway run", null, "Started the installed OpenClaw Gateway.");
        }

        var sourceRoot = ResolveSourceRoot();
        var pnpm = FindOnPath("pnpm.cmd") ?? FindOnPath("pnpm.exe");
        if (sourceRoot is not null && pnpm is not null)
        {
            return CreateLaunch(pnpm, "openclaw gateway run", sourceRoot, "Started the Gateway from the local OpenClaw checkout.");
        }

        return null;
    }

    private static (ProcessStartInfo StartInfo, string Description) CreateLaunch(
        string executable,
        string arguments,
        string? workingDirectory,
        string description)
    {
        var isCommandScript = string.Equals(
            Path.GetExtension(executable),
            ".cmd",
            StringComparison.OrdinalIgnoreCase);
        return (
            new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                UseShellExecute = isCommandScript,
                CreateNoWindow = !isCommandScript,
                WindowStyle = ProcessWindowStyle.Hidden,
            },
            description);
    }

    private static string? ResolveSourceRoot()
    {
        var environmentRoot = Environment.GetEnvironmentVariable("OPENCLAW_SOURCE_ROOT");
        if (IsSourceRoot(environmentRoot))
        {
            return environmentRoot;
        }

        var configPath = Path.Combine(AppContext.BaseDirectory, "openclaw-desktop.json");
        if (File.Exists(configPath))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(configPath));
                if (document.RootElement.TryGetProperty("sourceRoot", out var value))
                {
                    var configuredRoot = value.GetString();
                    if (IsSourceRoot(configuredRoot))
                    {
                        return configuredRoot;
                    }
                }
            }
            catch
            {
                // A malformed optional development config must not prevent app startup.
            }
        }

        return FindSourceRoot(new DirectoryInfo(Environment.CurrentDirectory))
            ?? FindSourceRoot(new DirectoryInfo(AppContext.BaseDirectory));
    }

    private static string? FindSourceRoot(DirectoryInfo? directory)
    {
        while (directory is not null)
        {
            if (IsSourceRoot(directory.FullName))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bool IsSourceRoot(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && File.Exists(Path.Combine(path, "openclaw.mjs"))
        && File.Exists(Path.Combine(path, "pnpm-workspace.yaml"));

    private static string? FindOnPath(string executable)
    {
        foreach (var segment in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator))
        {
            var directory = segment.Trim().Trim('"');
            if (directory.Length == 0)
            {
                continue;
            }

            var candidate = Path.Combine(directory, executable);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? FindInstalledCli()
    {
        var npmCli = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "npm",
            "openclaw.cmd");
        if (File.Exists(npmCli))
        {
            return npmCli;
        }

        return FindOnPath("openclaw.cmd") ?? FindOnPath("openclaw.exe");
    }

    public void Dispose()
    {
        _http.Dispose();
        _gatewayProcess?.Dispose();
    }
}
