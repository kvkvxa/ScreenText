using System.Text.Json;
using System.IO;
using ScreenText.Platform;

namespace ScreenText.Settings;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
    private readonly string _path;
    private readonly string? _legacyPath;
    private readonly StartupService _startupService = new();

    public SettingsService(string? path = null)
    {
        if (path is not null)
        {
            _path = path;
            _legacyPath = null;
        }
        else
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _path = Path.Combine(localAppData, "ScreenText", "settings.json");
            _legacyPath = Path.Combine(localAppData, "OCRTool", "settings.json");
        }
    }

    public AppSettings Load()
    {
        var readPath = File.Exists(_path) ? _path : _legacyPath is not null && File.Exists(_legacyPath) ? _legacyPath : null;
        try
        {
            if (readPath is null) return new AppSettings();
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(readPath), JsonOptions) ?? new AppSettings();
            settings.Normalize();
            return settings;
        }
        catch (JsonException)
        {
            var corruptPath = readPath is null
                ? _path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")
                : readPath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N");
            try
            {
                if (readPath is not null && File.Exists(readPath)) File.Move(readPath, corruptPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new AppSettings();
        }
        catch (System.Security.SecurityException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Normalize();
        var directory = Path.GetDirectoryName(_path) ?? throw new InvalidOperationException("Settings directory is unavailable.");
        Directory.CreateDirectory(directory);
        var temporaryPath = _path + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, settings, JsonOptions);
                stream.Flush(true);
            }
            File.Move(temporaryPath, _path, true);
        }
        catch (System.Security.SecurityException)
        {
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public void ApplyStartupSetting(bool enabled) => _startupService.SetEnabled(enabled);
}
