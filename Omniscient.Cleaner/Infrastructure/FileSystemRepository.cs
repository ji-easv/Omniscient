using System.Collections.Concurrent;
using Omniscient.Cleaner.Infrastructure.Interfaces;

namespace Omniscient.Cleaner.Infrastructure
{
    public class FileSystemRepository : IFileSystemRepository
    {
        private readonly ILogger<FileSystemRepository> _logger;
        private const int MaxConcurrency = 20;

        public FileSystemRepository(ILogger<FileSystemRepository> logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, string>> GetFiles(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogError("No path provided to search for files.");
                throw new Exception("No path provided to search for files.");
            }

            var filesDictionary = new ConcurrentDictionary<string, string>();
            var allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

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

                        filesDictionary[relativePath] = textContent;

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

            return new Dictionary<string, string>(filesDictionary);
        }
    }
}