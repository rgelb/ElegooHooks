# Elegoo Printer Events

Elegoo Printer Events is a Windows desktop application that launches your own
executables when printer events such as print started, print completed,
connection changes, or printer errors occur. It also connects to Elegoo printers
on the local network and displays their activity in a live per-printer event log.

The application is built with .NET 10 and Windows Forms. It uses the official
[Elegoo Link SDK](https://github.com/elegoo-repo/elegoo-link) through a native
.NET bridge included with the application.

## Features

- Save and monitor multiple printers by IP address.
- Discover supported printers automatically or select a printer type manually.
- Reconnect saved printers when the application starts.
- Display a separate live event log for each printer.
- Show event timestamps in local time with expandable event details.
- Retain up to 10,000 log entries per printer for the current session.
- Retry offline connections and remove printers from the application.
- Launch configurable executables for connection, print-lifecycle, and error
  events.
- Run entirely as the current Windows user without a background service.

## Printer compatibility

The application has been tested with the **Elegoo Centauri Carbon**.

The Elegoo Link SDK also identifies the following printer types, which should
theoretically work but have not been tested with this application:

- Centauri Carbon 2
- Neptune 4 Pro, Plus, and Max
- OrangeStorm Giga
- Generic Moonraker-compatible printers

Behavior may vary by printer model and firmware. Printer connections can also be
limited by the firmware, so close unnecessary slicer or browser sessions if the
application reports that no WebSocket connection is available.

## Install

Run the generated setup executable:

```text
artifacts\installer\ElegooPrinterEvents-Setup-1.0.0.exe
```

The installer is self-contained for Windows x64, so the target computer does not
need a separate .NET installation. It installs the application for the current
user and does not require administrator privileges.

## Using the desktop application

Start the application from the Start menu or run it from the repository:

```powershell
dotnet run --project .\src\ElegooLink.Desktop -c Release --no-build
```

Use **Add** to enter a printer IP address. The application attempts discovery
first. If discovery does not identify the printer, expand **Advanced** and
select its printer type manually. A printer is still saved when its initial
connection fails, allowing it to be retried later.

Select a printer in the left pane to view its events. Offline printers can be
retried from the printer list's context menu.

### Event automation

Use **Event Settings** to configure an executable for any supported connection,
print-lifecycle, or printer-error event. Each action supports:

- Enabled or disabled state
- Executable path
- Argument template
- Working directory
- Hidden-window execution

Executable actions are launched directly as the current user without
`cmd.exe`, PowerShell, shell interpretation, or elevation.

Argument templates support:

`{PrinterId}`, `{PrinterName}`, `{PrinterIp}`, `{Event}`, `{TimestampUtc}`,
`{Message}`, `{FileName}`, `{Progress}`, `{CurrentLayer}`, `{TotalLayers}`,
`{State}`, `{SubState}`, and `{ErrorCodes}`.

Missing values expand to an empty string. Quote placeholders in the argument
template when their values may contain spaces.

Initial printer-state observations are logged but do not launch automation.
Failures to start configured executables are added to the corresponding
printer's event log.

### Settings and logs

Printer definitions and automation rules are stored for the current Windows
user at:

```text
%LocalAppData%\ElegooHooks\settings.json
```

No credentials or event logs are stored. Logs remain in memory only for the
current application session. Access-code authentication is not currently
supported by the desktop application.

## Building from source

Requirements:

- Windows x64
- .NET 10 SDK or newer
- Visual Studio 2022 or newer with **Desktop development with C++**
- CMake 3.24 or newer
- Git

Build the native bridge and all .NET projects, then run the tests:

```powershell
.\scripts\build.ps1
```

The first build clones vcpkg into the ignored `.tools` directory, downloads the
pinned Elegoo Link SDK source, and compiles its native dependencies. The first
build therefore takes longer than subsequent builds.

The native `elegoo_link_bridge.dll` must be present beside
`ElegooLink.Desktop.exe`. The build and publishing scripts copy it into the
appropriate output directories automatically.

## Creating the installer

Install [Inno Setup 6](https://jrsoftware.org/isinfo.php), then run:

```powershell
.\scripts\build-installer.ps1
```

This performs a Release build and test run, publishes the self-contained Windows
x64 application, verifies the native bridge, and creates:

```text
artifacts\installer\ElegooPrinterEvents-Setup-1.0.0.exe
```

Specify a different release version with:

```powershell
.\scripts\build-installer.ps1 -Version 1.1.0
```

Use `-SkipBuild` only when the native bridge and Release build already exist and
you only need to publish and package the application.

### Creating a GitHub release

The **Build installer and create release** workflow can publish a release
manually:

1. Open the repository's **Actions** tab.
2. Select **Build installer and create release**.
3. Select **Run workflow**.
4. Enter a numeric version such as `1.0.0` and choose whether it is a prerelease.

The workflow builds the native bridge and .NET projects on Windows with Visual
Studio 2026, runs the tests, creates the installer, performs a silent
install/uninstall smoke test, and publishes the installer and its SHA-256
checksum in a GitHub Release tagged with the selected version.

## Optional console listener

The repository also contains a small console application for diagnostics and
scripted monitoring. To discover and monitor supported printers:

```powershell
dotnet run --project .\src\ElegooLink.EventConsole -c Release --no-build
```

Run it with `--help` to see direct-connect, JSON, raw-event, demo, and native-log
options.
