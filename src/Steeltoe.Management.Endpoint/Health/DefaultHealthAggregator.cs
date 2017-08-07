using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health
{
    public class DefaultHealthAggregator : IHealthAggregator
    {
        public Health Aggregate(IList<IHealthContributor> contributors)
        {
            if (contributors == null)
            {
                return new Health();
            }

            Health result = new Health();
            foreach(var contributor in contributors)
            {
                var h = contributor.Health();
                if (h.Status > result.Status)
                {
                    result.Status = h.Status;
                }
                result.Details.Add(contributor.Id, h.Details);
            }
            return result;
        }
    }
}
