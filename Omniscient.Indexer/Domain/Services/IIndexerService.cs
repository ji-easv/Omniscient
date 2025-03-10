using Omniscient.Shared;
using Omniscient.Shared.Dtos;
using Omniscient.Shared.Entities;

namespace Omniscient.Indexer.Domain.Services;

public interface IIndexerService
{
    Task<EmailDto> GetEmailAsync(Guid emailId);
    Task<PaginatedList<EmailDto>> SearchEmailsAsync(string query, int pageIndex, int pageSize);
    Task IndexEmail(Email email);
}