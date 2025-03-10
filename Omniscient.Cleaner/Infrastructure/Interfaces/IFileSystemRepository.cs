namespace Omniscient.Cleaner.Infrastructure.Interfaces;

public interface IFileSystemRepository
{
    Task ReadAndPublishFiles(string? path);
}