
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;

namespace FortuneTellerService.Models
{
    public class FortuneContext : DbContext
    {
        public FortuneContext(DbContextOptions<FortuneContext> options) :
            base(options)
        {

        }
        public DbSet<Fortune> Fortunes { get; set; }
    }
}
