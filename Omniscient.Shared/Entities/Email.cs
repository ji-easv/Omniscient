namespace Omniscient.Shared.Entities;

public class Email
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    public required string Content { get; set; }
}