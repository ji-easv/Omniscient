using Omniscient.Cleaner.Infrastructure.Interfaces;
using Omniscient.ServiceDefaults;

namespace Omniscient.Cleaner.Domain.Services;

public class FileSystemService(
    IFileSystemRepository fileSystemRepository,
    string[] args) : IHostedService, IFileSystemService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await ReadAndPublishFiles();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task ReadAndPublishFiles()
    {
        if (args.Contains("init") || args.Contains("--init"))
        {
            using var activity = ActivitySources.OmniscientActivitySource.StartActivity();

            var filePath = args
                .FirstOrDefault(arg => arg.StartsWith("--path=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--path=".Length);

            filePath ??= Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
                ".enron-files", "maildir");
            
            await fileSystemRepository.ReadAndPublishFiles(filePath);
        }
    }
}