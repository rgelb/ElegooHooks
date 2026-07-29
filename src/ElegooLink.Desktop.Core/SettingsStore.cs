using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElegooLink.Desktop.Core;

public sealed record SettingsLoadResult(
    AppSettings Settings,
    string? Warning = null,
    string? PreservedBadPath = null);

public interface ISettingsStore
{
    string SettingsPath { get; }

    Task<SettingsLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonSettingsStore(string? settingsPath = null)
    {
        SettingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ElegooHooks",
            "settings.json");
    }

    public string SettingsPath { get; }

    public async Task<SettingsLoadResult> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(SettingsPath))
        {
            return new SettingsLoadResult(SettingsNormalizer.Normalize(null));
        }

        try
        {
            await using var stream = File.OpenRead(SettingsPath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
            return new SettingsLoadResult(SettingsNormalizer.Normalize(settings));
        }
        catch (Exception exception) when (
            exception is JsonException or NotSupportedException)
        {
            var badPath = PreserveMalformedSettings();
            return new SettingsLoadResult(
                SettingsNormalizer.Normalize(null),
                $"The settings file could not be read and was preserved as '{badPath}'. " +
                "The application is starting with empty settings.",
                badPath);
        }
    }

    public async Task SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings = SettingsNormalizer.Normalize(settings);

        var directory = Path.GetDirectoryName(SettingsPath)
            ?? throw new InvalidOperationException("The settings path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(
            directory,
            $"{Path.GetFileName(SettingsPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    JsonOptions,
                    cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryPath, SettingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private string PreserveMalformedSettings()
    {
        var directory = Path.GetDirectoryName(SettingsPath) ?? "";
        var fileName = Path.GetFileName(SettingsPath);
        var badPath = Path.Combine(directory, $"{fileName}.bad");
        var suffix = 1;

        while (File.Exists(badPath))
        {
            badPath = Path.Combine(directory, $"{fileName}.{suffix}.bad");
            suffix++;
        }

        File.Move(SettingsPath, badPath);
        return badPath;
    }
}
