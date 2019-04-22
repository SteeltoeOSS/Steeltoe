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

using System.Collections.Generic;

namespace Steeltoe.Common.HealthChecks
{
    /// <summary>
    /// The result of a health check
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Gets or sets the status of the check
        /// </summary>
        /// <remarks>Used by HealthMiddleware to determine HTTP Status code</remarks>
        public HealthStatus Status { get; set; } = HealthStatus.UNKNOWN;

        /// <summary>
        /// Gets or sets a description of the health check result
        /// </summary>
        /// <remarks>Currently only used on check failures</remarks>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets details of the checked item
        /// </summary>
        /// <remarks>For parity with Spring Boot, repeat status [with a call to .ToString()] here</remarks>
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }
}