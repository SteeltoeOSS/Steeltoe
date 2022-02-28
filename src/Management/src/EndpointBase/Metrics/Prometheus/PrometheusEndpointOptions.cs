// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class PrometheusEndpointOptions : AbstractEndpointOptions, IPrometheusEndpointOptions
    {
        internal const string MANAGEMENT_INFO_PREFIX = "management:endpoints:prometheus";

        public PrometheusEndpointOptions()
            : base()
        {
            Id = "prometheus";
            ExactMatch = false;
        }

        public PrometheusEndpointOptions(IConfiguration config)
            : base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "prometheus";
            }

            ExactMatch = false;
        }

        public long ScrapeResponseCacheDurationMilliseconds { get; set; }
    }
}
