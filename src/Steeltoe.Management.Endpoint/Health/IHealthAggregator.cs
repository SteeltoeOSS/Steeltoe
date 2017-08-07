using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health
{
    public interface IHealthAggregator
    {
        Health Aggregate(IList<IHealthContributor> contributors);
    }
}
