using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;

namespace NexusMods.App.UI;

public sealed class LogUploadService
{
    private const int MaxFiles = 3;
    private static readonly TimeSpan Recent = TimeSpan.FromHours(1);

    private const string Server = "http://127.0.0.1:3000";
    private static readonly Uri UploadLogsEndpoint = new(Server + "/logs");
    private const string ViewLogBundleEndpoint = Server + "/view/bundle/";

    private readonly ILogger _logger;
    private readonly ISettingsManager _settingsManager;

    public LogUploadService(ILogger<LogUploadService> logger, ISettingsManager settingsManager)
    {
        _logger = logger;
        _settingsManager = settingsManager;
    }

    public async Task<Uri?> UploadLogs()
    {
        var loggingSettings = _settingsManager.Get<LoggingSettings>();
        var logDirectoryPath = loggingSettings.MainProcessLogFilePath.ToPath(FileSystem.Shared).Parent;

        var filesToUpload = logDirectoryPath
            .EnumerateFiles(recursive: false)
            .Select(path => path.FileInfo)
            .Where(fileInfo => fileInfo.LastWriteTime - DateTime.Now < Recent)
            .OrderByDescending(fileInfo => fileInfo.LastWriteTime)
            .Take(count: MaxFiles)
            .ToArray();

        var client = new HttpClient();

        var content = new MultipartFormDataContent();
        foreach (var file in filesToUpload)
        {
            var bytes = await file.Path.ReadAllBytesAsync();
            content.Add(new ByteArrayContent(bytes), name: file.Path.Name, fileName: file.Path.Name);
        }

        using var result = await client.PostAsync(UploadLogsEndpoint, content);
        result.EnsureSuccessStatusCode();

        var logBundleId = await result.Content.ReadAsStringAsync();
        return new Uri(ViewLogBundleEndpoint + logBundleId);
    }
}
