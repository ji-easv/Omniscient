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
        return await indexerRepository.SearchEmailsAsync(query, pageIndex, pageSize);
    }

    public async Task IndexEmail(Email email)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        
        // Add the email to the database
        await indexerRepository.AddEmailAsync(email);

        // Split the email content into words
        var wordList = email.Content.Split(' ');

        // Find all unique words in the email and add them to the database
        var words = wordList.Distinct().Select(w => new Word { Value = w }).ToList();
        await indexerRepository.AddWordsAsync(words);

        // For each word, find all occurrences in the email and add them to the database
        var occurrences = new List<Occurence>();
        foreach (var word in words)
        {
            var occurrenceCount = wordList.Count(w => w == word.Value);
            occurrences.Add(new Occurence
            {
                WordValue = word.Value,
                EmailId = email.Id,
                Count = occurrenceCount,
                Word = word,
                Email = email
            });
        }
        
        await indexerRepository.AddOccurrencesAsync(occurrences);
    }
}