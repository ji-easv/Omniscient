using Omniscient.Indexer.Infrastructure;
using Omniscient.ServiceDefaults;
using Omniscient.Shared;
using Omniscient.Shared.Dtos;
using Omniscient.Shared.Entities;
using Omniscient.Shared.Exceptions;
using Omniscient.Shared.Mappers;
using IIndexerRepository = Omniscient.Indexer.Infrastructure.Repository.IIndexerRepository;

namespace Omniscient.Indexer.Domain.Services;

public class IndexerService(IIndexerRepository indexerRepository, ILogger<IIndexerService> logger, AppDbContext context) : IIndexerService
{
   private readonly char[] _splitChars = [' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '|', '`', '~', '@', '#', '$', '%', '^', '&', '*', '-', '_', '+', '=', '"'];

    public async Task<EmailDto> GetEmailAsync(Guid emailId)
    {
        var email = await indexerRepository.GetEmailByIdAsync(emailId);

        if (email == null)
        {
            throw new NotFoundException($"Email with id {emailId} not found");
        }

        return email.ToDto();
    }

    public async Task<PaginatedList<EmailDto>> SearchEmailsAsync(string query, int pageIndex, int pageSize)
    {
        query = query.ToLower();
        var queryTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var emails = await indexerRepository.SearchEmailsAsync(queryTerms, pageIndex, pageSize);
        return emails.MapTo(e => e.ToDto());
    }

    public async Task IndexEmails(List<Email> emails)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();

        var uniqueWords = new HashSet<string>();
        var occurrences = new List<Occurence>();
        var emailsToSave = new List<Email>();

        foreach (var email in emails)
        {
            emailsToSave.Add(email);

            // Split the email content into words
            var wordDictionary = email.Content
                .Split(_splitChars, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.ToLower())
                .GroupBy(w => w)
                .ToDictionary(g => g.Key, g => g.Count());

            // For each word, find all occurrences in the email and add them to the database
            foreach (var (word, count) in wordDictionary)
            {
                uniqueWords.Add(word);

                occurrences.Add(new Occurence
                {
                    WordValue = word,
                    EmailId = email.Id,
                    Count = count
                });
            }
            //logger.LogInformation("Email with file name {EmailFileName} processed", email.FileName);
        }
        
        // Find all unique words in the email and add them to the database
        await indexerRepository.UpsertWordsAsync(uniqueWords.ToList());
        await indexerRepository.AddOccurrencesAsync(occurrences);
        await indexerRepository.AddEmailsAsync(emailsToSave);

        await context.SaveChangesAsync();
        GC.Collect();
    }

    public async Task<string> GetFullEmailContent(Guid emailId)
    {
        var email = await indexerRepository.GetEmailByIdAsync(emailId);
        if (email == null)
        {
            throw new NotFoundException($"Email with id {emailId} not found");
        }
        
        return email.Content;
    }
}