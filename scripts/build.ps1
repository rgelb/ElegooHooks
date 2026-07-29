[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipNative
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$nativeRoot = Join-Path $projectRoot "native"
$toolsRoot = Join-Path $projectRoot ".tools"
$vcpkgRoot = if ($env:VCPKG_ROOT) { $env:VCPKG_ROOT } else { Join-Path $toolsRoot "vcpkg" }

function Find-CMake {
    $command = Get-Command cmake -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path -LiteralPath $vswhere) {
        $installations = & $vswhere -all -products * -property installationPath
        foreach ($installation in $installations) {
            $candidate = Join-Path $installation "Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"
            if (Test-Path -LiteralPath $candidate) {
                return $candidate
            }
        }
    }

    throw "CMake 3.24+ was not found. Install it from https://cmake.org/download/ and rerun this script."
}

function Find-NativeToolchain {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path -LiteralPath $vswhere)) {
        throw "Visual Studio with the Desktop development with C++ workload was not found."
    }

    $json = & $vswhere `
        -latest `
        -products * `
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
        -format json
    $installation = $json | ConvertFrom-Json | Select-Object -First 1
    if (-not $installation) {
        throw "Install the Visual Studio 2022 or newer 'Desktop development with C++' workload, then rerun this script."
    }

    $major = [int]($installation.installationVersion -split '\.')[0]
    $generator = switch ($major) {
        18 { "Visual Studio 18 2026" }
        17 { "Visual Studio 17 2022" }
        default { throw "Visual Studio major version $major is not supported by this build script." }
    }

    return $generator
}

if (-not $SkipNative) {
    $cmake = Find-CMake
    $generator = Find-NativeToolchain
    if (-not ((& $cmake --help) -match [regex]::Escape($generator))) {
        throw "The installed CMake does not support '$generator'. Update CMake from https://cmake.org/download/."
    }

    $generatorSlug = $generator.ToLowerInvariant() -replace '[^a-z0-9]+', '-'
    $nativeBuild = Join-Path $toolsRoot "native-build\$generatorSlug"
    New-Item -ItemType Directory -Force -Path $toolsRoot | Out-Null

    if (-not (Test-Path -LiteralPath (Join-Path $vcpkgRoot "scripts\buildsystems\vcpkg.cmake"))) {
        if (Test-Path -LiteralPath $vcpkgRoot) {
            throw "VCPKG_ROOT '$vcpkgRoot' exists but is not a valid vcpkg checkout."
        }

        Write-Host "Cloning vcpkg..."
        git clone https://github.com/microsoft/vcpkg.git $vcpkgRoot
    }

    if (-not (Test-Path -LiteralPath (Join-Path $vcpkgRoot "vcpkg.exe"))) {
        & (Join-Path $vcpkgRoot "bootstrap-vcpkg.bat") -disableMetrics
        if ($LASTEXITCODE -ne 0) {
            throw "vcpkg bootstrap failed with exit code $LASTEXITCODE."
        }
    }

    $toolchain = Join-Path $vcpkgRoot "scripts\buildsystems\vcpkg.cmake"
    Write-Host "Configuring the Elegoo Link SDK and native .NET bridge..."
    & $cmake `
        --no-warn-unused-cli `
        -S $nativeRoot `
        -B $nativeBuild `
        -G $generator `
        -A x64 `
        "-DCMAKE_TOOLCHAIN_FILE=$toolchain" `
        "-DVCPKG_TARGET_TRIPLET=x64-windows-static-md"
    if ($LASTEXITCODE -ne 0) {
        throw "Native configuration failed with exit code $LASTEXITCODE."
    }

    & $cmake --build $nativeBuild --config $Configuration --target elegoo_link_bridge
    if ($LASTEXITCODE -ne 0) {
        throw "Native build failed with exit code $LASTEXITCODE."
    }

    $nativeDll = Join-Path $nativeBuild "bin\$Configuration\elegoo_link_bridge.dll"
    if (-not (Test-Path -LiteralPath $nativeDll)) {
        throw "Native build completed but '$nativeDll' was not produced."
    }

    $artifactDirectory = Join-Path $nativeRoot "artifacts\$Configuration"
    New-Item -ItemType Directory -Force -Path $artifactDirectory | Out-Null
    Copy-Item -LiteralPath $nativeDll -Destination $artifactDirectory -Force
}

Write-Host "Building and testing .NET projects..."
dotnet build (Join-Path $projectRoot "ElegooHooks.sln") --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    throw ".NET build failed with exit code $LASTEXITCODE."
}

dotnet test (Join-Path $projectRoot "ElegooHooks.sln") --configuration $Configuration --no-build
if ($LASTEXITCODE -ne 0) {
    throw ".NET tests failed with exit code $LASTEXITCODE."
}

Write-Host ""
Write-Host "Build complete."
Write-Host "Run console: dotnet run --project src/ElegooLink.EventConsole -c $Configuration --no-build"
Write-Host "Run desktop: dotnet run --project src/ElegooLink.Desktop -c $Configuration --no-build"
