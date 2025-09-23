using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Dtos.Public; // hvis du lægger en tynd AreaDto her
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Public.Areas;


namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class AreasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<AreaDto>> Get(CancellationToken ct)
        => await db.Areas.AsNoTracking()
           .Select(a => new AreaDto(a.Name, a.Houses.Count))
           .ToListAsync(ct);
}
