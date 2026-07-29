[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(?:\.\d+)?$')]
    [string]$Version = "1.0.0",
    [switch]$SkipBuild,
    [string]$InnoCompiler
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$desktopProject = Join-Path $projectRoot "src\ElegooLink.Desktop\ElegooLink.Desktop.csproj"
$publishProfile = Join-Path $projectRoot "src\ElegooLink.Desktop\Properties\PublishProfiles\FolderProfile.pubxml"
$publishDirectory = Join-Path $projectRoot "src\ElegooLink.Desktop\bin\Release\net10.0-windows\publish\win-x64"
$installerScript = Join-Path $projectRoot "installer\ElegooPrinterEvents.iss"
$installerOutputDirectory = Join-Path $projectRoot "artifacts\installer"
$installerFileName = "ElegooPrinterEvents-Setup-$Version.exe"

function Resolve-InnoCompiler {
    param([string]$RequestedPath)

    if ($RequestedPath) {
        $resolvedPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RequestedPath)
        if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
            throw "The Inno Setup compiler was not found at '$resolvedPath'."
        }

        return $resolvedPath
    }

    $command = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }

    throw "Inno Setup 6 was not found. Install it or pass -InnoCompiler with the path to ISCC.exe."
}

if (-not $SkipBuild) {
    Write-Host "Building and testing the application..."
    & (Join-Path $PSScriptRoot "build.ps1") -Configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "The Release build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $publishProfile -PathType Leaf)) {
    throw "The desktop publish profile was not found at '$publishProfile'."
}

Write-Host "Publishing the self-contained Windows x64 application..."
dotnet publish $desktopProject `
    --configuration Release `
    "-p:PublishProfile=$publishProfile" `
    "-p:Version=$Version"
if ($LASTEXITCODE -ne 0) {
    throw "Desktop publishing failed with exit code $LASTEXITCODE."
}

$requiredFiles = @(
    (Join-Path $publishDirectory "ElegooLink.Desktop.exe"),
    (Join-Path $publishDirectory "elegoo_link_bridge.dll")
)

foreach ($requiredFile in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Publishing completed, but the required file '$requiredFile' is missing."
    }
}

$compiler = Resolve-InnoCompiler -RequestedPath $InnoCompiler
New-Item -ItemType Directory -Force -Path $installerOutputDirectory | Out-Null

Write-Host "Compiling the installer..."
$compilerArguments = @(
    "/DMyAppVersion=$Version",
    "/DPublishDir=$publishDirectory",
    "/DInstallerOutputDir=$installerOutputDirectory",
    $installerScript
)

& $compiler @compilerArguments
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

$installerPath = Join-Path $installerOutputDirectory $installerFileName
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw "Inno Setup completed, but '$installerPath' was not produced."
}

$installer = Get-Item -LiteralPath $installerPath
$hash = Get-FileHash -LiteralPath $installerPath -Algorithm SHA256

Write-Host ""
Write-Host "Installer created successfully."
Write-Host "Path: $($installer.FullName)"
Write-Host "Size: $([Math]::Round($installer.Length / 1MB, 2)) MB"
Write-Host "SHA256: $($hash.Hash)"
