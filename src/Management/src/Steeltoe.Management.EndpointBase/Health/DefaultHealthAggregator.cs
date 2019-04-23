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

using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;

namespace Steeltoe.Management.Endpoint.Health
{
    public class DefaultHealthAggregator : IHealthAggregator
    {
        public HealthCheckResult Aggregate(IList<IHealthContributor> contributors)
        {
            if (contributors == null)
            {
                return new HealthCheckResult();
            }

            var result = new HealthCheckResult();
            foreach (var contributor in contributors)
            {
                HealthCheckResult h = null;
                try
                {
                    h = contributor.Health();
                }
                catch (Exception)
                {
                    h = new HealthCheckResult();
                }

                if (h.Status > result.Status)
                {
                    result.Status = h.Status;
                }

                string key = GetKey(result, contributor.Id);
                result.Details.Add(key, h.Details);
            }

            return result;
        }

        protected static string GetKey(HealthCheckResult result, string key)
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
