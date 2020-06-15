// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.MetricsStream
{
    public class HystrixMetricsStreamOptions
    {
        public int GatherRate { get; set; } = 500;

        public int SendRate { get; set; } = 500;

        public bool Validate_Certificates { get; set; } = true;
    }
}
