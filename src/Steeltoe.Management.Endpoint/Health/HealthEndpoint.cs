
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthEndpoint : AbstractEndpoint<Health>
    {
        private IHealthAggregator _aggregator;
        private IList<IHealthContributor> _contributors;
        private ILogger<HealthEndpoint> _logger;

        public new IHealthOptions Options
        {
            get
            {
                return options as IHealthOptions;
            }
        }

        public HealthEndpoint(IHealthOptions options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors, ILogger<HealthEndpoint> logger)
           : base(options)
        {
            _aggregator = aggregator;
            _contributors = contributors.ToList();
            _logger = logger;
        }

        public override Health Invoke()
        {
            return BuildHealth(_aggregator, _contributors);
         
        }

        protected virtual Health BuildHealth(IHealthAggregator aggregator, IList<IHealthContributor> contributors)
        {
            return _aggregator.Aggregate(contributors);
        }

    }
}
