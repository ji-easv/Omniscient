namespace Omniscient.Cleaner.Infrastructure.Interfaces;

public interface IFileSystemRepository
{
    Task<Dictionary<string, string>> GetFiles(string? path);
    Task ReadAndPublishFiles(string? path);
}