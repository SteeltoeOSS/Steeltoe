// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

        public HealthEndpoint(IHealthOptions options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors, ILogger<HealthEndpoint> logger = null)
           : base(options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (aggregator == null)
            {
                throw new ArgumentNullException(nameof(aggregator));
            }

            if (contributors == null)
            {
                throw new ArgumentNullException(nameof(contributors));
            }

            _aggregator = aggregator;
            _contributors = contributors.ToList();
            _logger = logger;
        }

        public new IHealthOptions Options
        {
            get
            {
                return options as IHealthOptions;
            }
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
