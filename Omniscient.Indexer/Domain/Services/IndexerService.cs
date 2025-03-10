using Omniscient.ServiceDefaults;
using Omniscient.Shared;
using Omniscient.Shared.Dtos;
using Omniscient.Shared.Entities;
using Omniscient.Shared.Exceptions;
using Omniscient.Shared.Mappers;
using IIndexerRepository = Omniscient.Indexer.Infrastructure.Repository.IIndexerRepository;

namespace Omniscient.Indexer.Domain.Services;

public class IndexerService(IIndexerRepository indexerRepository, ILogger<IIndexerService> logger) : IIndexerService
{
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

    public async Task IndexEmail(Email email)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();
        
        // Add the email to the database
        var existingEmail = await indexerRepository.GetEmailByFileName(email.FileName);
        if (existingEmail != null)
        {
            logger.LogInformation("Email with file name {EmailFileName} already exists, deleting the existing email", email.FileName);
            await indexerRepository.DeleteEmailAsync(existingEmail);
        }
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