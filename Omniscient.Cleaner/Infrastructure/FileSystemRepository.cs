using Omniscient.Cleaner.Infrastructure.Interfaces;
using Omniscient.Cleaner.Utilities;
using Omniscient.RabbitMQClient.Interfaces;
using Omniscient.RabbitMQClient.Messages;
using Omniscient.Shared.Entities;

namespace Omniscient.Cleaner.Infrastructure
{
    public class FileSystemRepository : IFileSystemRepository
    {
        private readonly ILogger<FileSystemRepository> _logger;
        private readonly IAsyncPublisher _publisher;

        private const int MaxConcurrency = 40;

        public FileSystemRepository(ILogger<FileSystemRepository> logger, IAsyncPublisher publisher)
        {
            _logger = logger;
            _publisher = publisher;
        }

        public async Task ReadAndPublishFiles(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogError("No path provided to search for files.");
                throw new Exception("No path provided to search for files.");
            }

            var allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Where(file => !Path.GetFileName(file).Equals(".DS_Store", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            _logger.LogInformation($"Found {allFiles.Length} files in {path}.");

            var semaphore = new SemaphoreSlim(MaxConcurrency);
            var tasks = new List<Task>();

            int processedFiles = 0;
            int lastLoggedPercentage = 0;
            var logLock = new object();

            foreach (var file in allFiles)
            {
                await semaphore.WaitAsync();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        var relativePath = Path.GetRelativePath(path, file);

                        var textContent = await File.ReadAllTextAsync(file);

                        await _publisher.PublishAsync(new EmailMessage()
                        {
                            Email = new Email()
                            {
                                Id = Guid.CreateVersion7(),
                                Content = EmailStringCleaner.RemoveHeaders(textContent),
                                FileName = relativePath,
                            }
                        });

                        int current = Interlocked.Increment(ref processedFiles);
                        int percentage = (current * 100) / allFiles.Length;

                        lock (logLock)
                        {
                            if (percentage - lastLoggedPercentage >= 2)
                            {
                                lastLoggedPercentage = percentage;
                                _logger.LogDebug($"{percentage}% complete ({current}/{allFiles.Length}) files read");
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }
    }
}