using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Steeltoe.Connector.Example.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly GoodDbContext _context;
        private readonly IConfiguration _config;

        public ValuesController(ILogger<ValuesController> logger, IConfiguration config, GoodDbContext context)
        {
            _logger = logger;
            _config = config;
            _context = context;
        }

        // GET: api/Tests1
        [HttpGet]
        public async Task<bool> Get()
        {
            return await _context.Database.CanConnectAsync();
        }
    }
}
