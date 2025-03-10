using Microsoft.AspNetCore.Mvc;
using Omniscient.Indexer.Domain.Services;
using Omniscient.Shared.Exceptions;

namespace Omniscient.Indexer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndexerController(IIndexerService indexerService) : ControllerBase
{
    [HttpGet("{emailId:guid}")]
    public async Task<IActionResult> GetEmail(Guid emailId)
    {
        try
        {
            var email = await indexerService.GetEmailAsync(emailId);
            return Ok(email);
        } 
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> GetEmailsPaginated([FromQuery] string query = "", [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    { 
        if (pageIndex < 1)
        {
            return BadRequest("Page index must be greater than 0.");
        }
        
        if (pageSize < 1)
        {
            return BadRequest("Page size must be greater than 0.");
        }
        
        var emails = await indexerService.SearchEmailsAsync(query, pageIndex, pageSize);
        return Ok(emails);
    }
}