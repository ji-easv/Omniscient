using Microsoft.EntityFrameworkCore;

namespace Omniscient.Shared.Entities;

[PrimaryKey(nameof(WordValue), nameof(EmailId))]
public class Occurence
{
    public required string WordValue { get; set; }
    public Word? Word { get; set; }
    public Guid EmailId { get; set; }
    public Email? Email { get; set; }
    public int Count { get; set; }
}