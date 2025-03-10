using Omniscient.ServiceDefaults;
using Omniscient.Shared;
using Omniscient.Shared.Entities;
using IIndexerRepository = Omniscient.Indexer.Infrastructure.Repository.IIndexerRepository;

namespace Omniscient.Indexer.Domain.Services;

public class IndexerService(IIndexerRepository indexerRepository) : IIndexerService
{
    public async Task<Email> GetEmailAsync(Guid emailId)
    {
        var email = await indexerRepository.GetEmailAsync(emailId);

        if (email == null)
        {
            throw new Exception($"Email with id {emailId} not found");
        }

        return email;
    }

    public async Task<PaginatedList<Email>> SearchEmailsAsync(string query, int pageIndex, int pageSize)
    {
        query = query.ToLower();
        var queryTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return await indexerRepository.SearchEmailsAsync(queryTerms, pageIndex, pageSize);
    }

    public async Task IndexEmail(Email email)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        
        // Add the email to the database
        await indexerRepository.AddEmailAsync(email);

        // Split the email content into words
        var splitChars = new[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']', '{', '}', '<', '>', '/', '\\', '|', '`', '~', '@', '#', '$', '%', '^', '&', '*', '-', '_', '+', '=', '"' };
        var wordList = email.Content.Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Select(w => w.ToLower()).ToList();

        // Find all unique words in the email and add them to the database
        var uniqueWordValues = wordList.Distinct().ToList();
        await indexerRepository.UpsertWordsAsync(uniqueWordValues);

        // For each word, find all occurrences in the email and add them to the database
        var occurrences = new List<Occurence>();
        foreach (var word in uniqueWordValues)
        {
            var occurrenceCount = wordList.Count(w => w == word);
            
            occurrences.Add(new Occurence
            {
                WordValue = word,
                EmailId = email.Id,
                Count = occurrenceCount
            });
        }
        
        await indexerRepository.AddOccurrencesAsync(occurrences);
    }
}