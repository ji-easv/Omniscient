using Omniscient.Shared.Dtos;
using Omniscient.Shared.Entities;

namespace Omniscient.Shared.Mappers;

public static class EmailMapper
{
    public static EmailDto ToDto(this Email email)
    {
        return new EmailDto
        {
            Id = email.Id,
            FileName = email.FileName,
            ContentPreview = email.Content[..Math.Min(email.Content.Length, 100)]
        };
    }
    
    public static Email ToEntity(this EmailDto emailDto)
    {
        return new Email
        {
            Id = emailDto.Id,
            FileName = emailDto.FileName,
            Content = emailDto.ContentPreview
        };
    }
}