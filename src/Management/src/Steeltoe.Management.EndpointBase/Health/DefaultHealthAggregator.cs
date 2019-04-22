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

using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;

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

                result.Details.Add(contributor.Id, h.Details);
            }

            return result;
        }
    }
}
