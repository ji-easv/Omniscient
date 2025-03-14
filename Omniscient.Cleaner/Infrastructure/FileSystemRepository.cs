using Microsoft.Extensions.Options;
using Omniscient.Cleaner.Infrastructure.Interfaces;
using Omniscient.Cleaner.Utilities;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Omniscient.ServiceDefaults;
using Omniscient.Shared.Entities;

namespace Omniscient.Cleaner.Infrastructure;

public class FileSystemRepository : IFileSystemRepository
{
    private readonly ILogger<FileSystemRepository> _logger;
    private readonly IAsyncPublisher _publisher;
    private readonly IOptions<FileSystemOptions> _options;
    private readonly object _logLock = new();

    private const int BatchSize = 5000;
    private const int MaxConcurrency = 20;
    private readonly SemaphoreSlim _semaphore = new(MaxConcurrency);

    private int _processedFiles = 0;
    private int _lastLoggedPercentage = 0;
    private int _allFilesCount = 0;
    private string _path = string.Empty;

    public FileSystemRepository(ILogger<FileSystemRepository> logger, IAsyncPublisher publisher, IOptions<FileSystemOptions> options)
    {
        _logger = logger;
        _publisher = publisher;
        _options = options;
    }

    public async Task ReadAndPublishFiles(string? path)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();

        if (string.IsNullOrEmpty(path))
        {
            _logger.LogError("No path provided to search for files.");
            throw new Exception("No path provided to search for files.");
        }

        var allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
            .Where(file => !Path.GetFileName(file).Equals(".DS_Store", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // To not spend long time waiting for fetching all the files we limit this to download 4000 mails, and if you want to fetch all provide --no-limit
        if (_options.Value.LimitFiles)
        {
            allFiles = allFiles
                .Take(4000)
                .ToArray();
        }

        _allFilesCount = allFiles.Length;
        _path = path;

        _logger.LogInformation($"Found {_allFilesCount} files in {path}.");

        for (int i = 0; i < allFiles.Length; i += BatchSize)
        {
            var batchFiles = allFiles.Skip(i).Take(BatchSize).ToArray();

            var emailTasks = batchFiles.Select(file => ProcessFileAsync(file));
            var emails = await Task.WhenAll(emailTasks);

            var emailMessage = new EmailMessage
            {
                Emails = emails.ToList()
            };
            await _publisher.PublishAsync(emailMessage);

            // Update progress.
            Interlocked.Add(ref _processedFiles, batchFiles.Length);
            int current = _processedFiles;
            int percentage = (current * 100) / _allFilesCount;

            lock (_logLock)
            {
                if (percentage - _lastLoggedPercentage >= 2)
                {
                    _lastLoggedPercentage = percentage;
                    _logger.LogDebug($"{percentage}% complete ({current}/{_allFilesCount}) files read");
                }
            }
        }
    }

    private async Task<Email> ProcessFileAsync(string file)
    {
        await _semaphore.WaitAsync();

        try
        {
            using var activity = ActivitySources.OmniscientActivitySource.StartActivity();

            var relativePath = Path.GetRelativePath(_path, file);
            var textContent = await File.ReadAllTextAsync(file);

            return new Email
            {
                Id = Guid.CreateVersion7(),
                Content = EmailStringCleaner.RemoveHeaders(textContent),
                FileName = relativePath,
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }
}