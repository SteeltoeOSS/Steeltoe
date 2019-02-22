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

using Steeltoe.Common.LoadBalancer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Common.Http.LoadBalancer.Test
{
    /// <summary>
    /// A bad fake load balancer that only resolves requests for "replaceme" as "someresolvedhost"
    /// </summary>
    internal class FakeLoadBalancer : ILoadBalancer
    {
        internal List<Tuple<Uri, Uri, TimeSpan, Exception>> Stats = new List<Tuple<Uri, Uri, TimeSpan, Exception>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeLoadBalancer"/> class.
        /// Only capable of resolving requests for "replaceme" as "someresolvedhost"
        /// </summary>
        public FakeLoadBalancer()
        {
        }

        public Task<Uri> ResolveServiceInstanceAsync(Uri request)
        {
            return Task.FromResult(new Uri(request.AbsoluteUri.Replace("replaceme", "someresolvedhost")));
        }

        public Task UpdateStatsAsync(Uri originalUri, Uri resolvedUri, TimeSpan responseTime, Exception exception)
        {
            Stats.Add(new Tuple<Uri, Uri, TimeSpan, Exception>(originalUri, resolvedUri, responseTime, exception));
            return Task.CompletedTask;
        }
    }
}
