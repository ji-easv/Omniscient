namespace Omniscient.Web.Models;

public class Email
{
    public Guid EmailId { get; set; }
    public required string FileName { get; set; }
    public required string FullContent { get; set; }
}