using Microsoft.EntityFrameworkCore;
using Steeltoe.Connector.SqlServer.EFCore;

namespace Steeltoe.Connector.Example
{
    public class CMSComm : BackgroundService, ICMSComm
    {
        private readonly ILogger _logger;
        private GoodDbContext _dbContext;
        private DbContextOptionsBuilder<GoodDbContext> dbBuilder;
        public CMSComm(ILogger<CMSComm> logger, IConfiguration config)
        {
            dbBuilder = new DbContextOptionsBuilder<GoodDbContext>().UseSqlServer(config);
            _logger = logger;
        }

        public async Task<bool> DbOnline()
        {
            _dbContext = new GoodDbContext(dbBuilder.Options);
            Console.WriteLine(_dbContext.Database.GetConnectionString());
            return _dbContext.Database.CanConnect();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, e.Message);
            }
        }
    }
}
