# OpenClaw Desktop for Windows

Native WPF shell for the OpenClaw Control UI. The app shows a native splash screen, starts or connects to the local Gateway, and hosts the dashboard in WebView2.

## Build

```powershell
powershell -ExecutionPolicy Bypass -File apps/windows/build.ps1
```

The installer is written to `apps/windows/artifacts/OpenClaw-Desktop-Setup-x64.exe`.

The installed app first looks for an existing Gateway at `http://127.0.0.1:18789/`. If it is offline, it tries the OpenClaw CLI installed under the user's npm directory or available on `PATH`.
