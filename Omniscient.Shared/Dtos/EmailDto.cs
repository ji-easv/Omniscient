using System.ComponentModel.DataAnnotations;

namespace Omniscient.Shared.Dtos;

public class EmailDto
{
    public Guid Id { get; set; }
    public required string FileName { get; set; }
    
    [MaxLength(100)]
    public required string ContentPreview { get; set; }
}