using Microsoft.AspNetCore.Mvc;
using Omniscient.Indexer.Domain.Services;

namespace Omniscient.Indexer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndexerController(IIndexerService indexerService) : ControllerBase
{
    [HttpGet("{emailId:guid}")]
    public async Task<IActionResult> GetEmail(Guid emailId)
    {
        var email = await indexerService.GetEmailAsync(emailId);
        return Ok(email);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetEmailsPaginated([FromQuery] string search = "", [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    { 
        if (pageIndex < 1)
        {
            return BadRequest("Page index must be greater than 0.");
        }
        
        if (pageSize < 1)
        {
            return BadRequest("Page size must be greater than 0.");
        }
        
        var emails = await indexerService.SearchEmailsAsync(search, pageIndex, pageSize);
        return Ok(emails);
    }
}