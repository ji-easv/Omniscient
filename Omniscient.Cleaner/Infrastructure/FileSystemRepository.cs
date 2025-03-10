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
    private readonly SemaphoreSlim _semaphore = new(MaxConcurrency);
    private readonly object _logLock = new();
   
    private const int MaxConcurrency = 20;

    private int _processedFiles = 0;
    private int _lastLoggedPercentage = 0;
    private int _allFilesCount = 0;
    private string _path = string.Empty;

    public FileSystemRepository(ILogger<FileSystemRepository> logger, IAsyncPublisher publisher)
    {
        _logger = logger;
        _publisher = publisher;
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

        _logger.LogInformation($"Found {allFiles.Length} files in {path}.");

        var tasks = new List<Task>();
        _allFilesCount = allFiles.Length;
        _path = path;

        foreach (var file in allFiles.Take(10)) // TODO: revert Take(10)
        {
            tasks.Add(ProcessSingleFile(file));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ProcessSingleFile(string file)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        await _semaphore.WaitAsync();

        try
        {
            var relativePath = Path.GetRelativePath(_path, file);

            var textContent = await File.ReadAllTextAsync(file);

            await _publisher.PublishAsync(new EmailMessage
            {
                Email = new Email
                {
                    Id = Guid.CreateVersion7(),
                    Content = EmailStringCleaner.RemoveHeaders(textContent),
                    FileName = relativePath,
                }
            });

            int current = Interlocked.Increment(ref _processedFiles);
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
        finally
        {
            _semaphore.Release();
        }
    }
}