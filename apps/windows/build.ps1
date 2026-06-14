param(
  [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$Project = Join-Path $PSScriptRoot "OpenClaw.Desktop\OpenClaw.Desktop.csproj"
$Publish = Join-Path $PSScriptRoot "publish"
$Artifacts = Join-Path $PSScriptRoot "artifacts"

$publishFullPath = [IO.Path]::GetFullPath($Publish)
$windowsAppRoot = [IO.Path]::GetFullPath($PSScriptRoot) + [IO.Path]::DirectorySeparatorChar
if (-not $publishFullPath.StartsWith($windowsAppRoot, [StringComparison]::OrdinalIgnoreCase)) {
  throw "Publish directory must stay inside apps/windows."
}
if (Test-Path $publishFullPath) {
  Remove-Item -LiteralPath $publishFullPath -Recurse -Force
}

dotnet restore $Project
dotnet publish $Project -c Release -r win-x64 --self-contained true -o $Publish

if ($SkipInstaller) {
  Write-Host "Published: $Publish"
  exit 0
}

$iscc = Get-Command iscc.exe -ErrorAction SilentlyContinue
if (-not $iscc) {
  $candidates = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe"),
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path ${env:ProgramFiles} "Inno Setup 7\ISCC.exe"),
    (Join-Path ${env:ProgramFiles} "Inno Setup 6\ISCC.exe")
  )
  foreach ($candidate in $candidates) {
    if (Test-Path $candidate) {
      $iscc = Get-Item $candidate
      break
    }
  }
}

if (-not $iscc) {
  throw "Inno Setup 7 is required. Install it with: winget install --id JRSoftware.InnoSetup.7 -e -s winget"
}

New-Item -ItemType Directory -Force -Path $Artifacts | Out-Null
& $iscc.FullName (Join-Path $PSScriptRoot "installer\OpenClaw.Desktop.iss")
Write-Host "Installer: $(Join-Path $Artifacts 'OpenClaw-Desktop-Setup-x64.exe')"
