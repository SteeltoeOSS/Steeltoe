using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Steeltoe.Connector.Example;

public class GoodDbContext : DbContext
{
    public GoodDbContext(DbContextOptions<GoodDbContext> options)
        : base(options)
    {
    }
}
