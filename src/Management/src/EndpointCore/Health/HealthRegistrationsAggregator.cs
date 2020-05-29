// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthRegistrationsAggregator : IHealthRegistrationsAggregator
    {
        private DefaultHealthAggregator _aggregator;
        private DefaultAsyncHealthAggregator _asyncAggregator;

        public HealthRegistrationsAggregator()
        {
            _aggregator = new DefaultHealthAggregator();
            _asyncAggregator = new DefaultAsyncHealthAggregator();
        }

        public async Task<HealthCheckResult> Aggregate(IEnumerable<IHealthContributor> contributors, IEnumerable<IAsyncHealthContributor> asyncContributors, IOptionsMonitor<HealthCheckServiceOptions> healthServiceOptions, IServiceProvider serviceProvider)
        {
            HealthCheckResult result = null;

            if (contributors != null)
            {
                result = _aggregator.Aggregate(contributors.ToList());
            }

            if (asyncContributors != null)
            {
                var asyncResult = await _asyncAggregator.Aggregate(asyncContributors);
                result = result == null ? asyncResult : result.Merge(asyncResult);
            }

            if (healthServiceOptions != null)
            {
                var registrationsResult = await AggregateRegistrations(healthServiceOptions, serviceProvider);
                result = result == null ? registrationsResult : result.Merge(registrationsResult);
            }

            return result;
        }

        private async Task<HealthCheckResult> AggregateRegistrations(IOptionsMonitor<HealthCheckServiceOptions> healthServiceOptions, IServiceProvider serviceProvider)
        {
            var result = new HealthCheckResult();
            foreach (var registration in healthServiceOptions.CurrentValue.Registrations)
            {
                HealthCheckResult h = await registration.CheckHealthAsync(serviceProvider);

                if (h.Status > result.Status)
                {
                    result.Status = h.Status;
                }

                var key = GetKey(result, registration.Name);
                result.Details.Add(key, h);
            }

            return result;
        }

        private string GetKey(HealthCheckResult result, string key)
        {
            // add the contribtor with a -n appended to the id
            if (result.Details.ContainsKey(key))
            {
                return string.Concat(key, "-", result.Details.Count(k => k.Key == key));
            }

            return key;
        }
    }
}
