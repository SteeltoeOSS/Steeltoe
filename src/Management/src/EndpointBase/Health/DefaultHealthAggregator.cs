// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
                HealthCheckResult h;
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

                var key = GetKey(result, contributor.Id);
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
