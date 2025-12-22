using Microsoft.AspNetCore.Mvc;
using OpenBioCardServer.Data;

namespace OpenBioCardServer.Controllers.Classic;

[Route("classic")]
[ApiController]
public class ClassicSystemController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClassicAuthController> _logger;

    public ClassicSystemController(AppDbContext context,
        ILogger<ClassicAuthController> logger)
    {
        _context = context;
        _logger = logger;
    }
}