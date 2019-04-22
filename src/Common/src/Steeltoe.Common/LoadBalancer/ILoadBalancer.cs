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

using System;
using System.Threading.Tasks;

namespace Steeltoe.Common.LoadBalancer
{
    public interface ILoadBalancer
    {
        /// <summary>
        /// Evaluates a Uri for a host name that can be resolved into a service instance
        /// </summary>
        /// <param name="request">A Uri containing a service name that can be resolved into one or more service instances</param>
        /// <returns>The original Uri, with serviceName replaced by the host:port of a service instance</returns>
        Task<Uri> ResolveServiceInstanceAsync(Uri request);

        /// <summary>
        /// A mechanism for tracking statistics for service instances
        /// </summary>
        /// <param name="originalUri">The original request Uri</param>
        /// <param name="resolvedUri">The Uri resolved by the load balancer</param>
        /// <param name="responseTime">The amount of time taken for a remote call to complete</param>
        /// <param name="exception">Any exception called during calls to a resolved service instance</param>
        /// <returns>A task</returns>
        Task UpdateStatsAsync(Uri originalUri, Uri resolvedUri, TimeSpan responseTime, Exception exception);
    }
}
