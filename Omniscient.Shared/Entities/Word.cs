using System.ComponentModel.DataAnnotations;

namespace Omniscient.Shared.Entities;

public class Word
{
    [Key]
    public required string Value { get; set; }
}