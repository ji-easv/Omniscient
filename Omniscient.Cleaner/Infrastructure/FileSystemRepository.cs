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
        if (string.IsNullOrEmpty(path))
        {
            _logger.LogError("No path provided to search for files.");
            throw new Exception("No path provided to search for files.");
        }

        _path = path;

        // Get all direct folders inside the path
        var directFolders = Directory.GetDirectories(path);
        _logger.LogInformation($"Found {directFolders.Length} sender folders in {path}.");
        var i = 0;
        // Process each sender folder
        foreach (var senderFolder in directFolders)
        {
            var senderName = Path.GetFileName(senderFolder);
            _logger.LogDebug($"Processing sender folder: {senderName}");
            
            // Get all email files in this sender folder (including subfolders)
            var senderFiles = Directory.GetFiles(senderFolder, "*.*", SearchOption.AllDirectories)
                .Where(file => !Path.GetFileName(file).Equals(".DS_Store", StringComparison.OrdinalIgnoreCase))
                .Take(20)
                .ToArray();
            
            _allFilesCount = senderFiles.Length;
            _logger.LogDebug($"Found {_allFilesCount} emails for sender {senderName}");
            _processedFiles = 0;
            _lastLoggedPercentage = 0;
            
            var emailTasks = senderFiles.Select(file => ProcessFileAsync(file));
            var emails = await Task.WhenAll(emailTasks);

            var emailMessage = new EmailMessage
            {
                Emails = emails.ToList(),
                Sender = senderName
            };
            await _publisher.PublishAsync(emailMessage);
            i++;

            // Update progress for this sender
            Interlocked.Add(ref _processedFiles, senderFiles.Length);
            _processedFiles += senderFiles.Length;

            lock (_logLock)
            {
                _logger.LogDebug($"Sender {senderName}: complete ({_allFilesCount}) files read");
            }
        }
        _logger.LogInformation($"Processed {_processedFiles} files from {_allFilesCount} total files.");
        _logger.LogInformation($"Finished processing files. {i}");
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