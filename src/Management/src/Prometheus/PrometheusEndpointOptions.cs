// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Prometheus;

public class PrometheusEndpointOptions : EndpointOptionsBase, IPrometheusEndpointOptions
{
    internal const string ManagementInfoPrefix = "management:endpoints:prometheus";

    public long ScrapeResponseCacheDurationMilliseconds { get; set; }
    public override bool ExactMatch => false;

    public PrometheusEndpointOptions()
    {
        Id = "prometheus";
    }

    //public PrometheusEndpointOptions(IConfiguration configuration)
    //    : base(ManagementInfoPrefix, configuration)
    //{
    //    if (string.IsNullOrEmpty(Id))
    //    {
    //        Id = "prometheus";
    //    }

    //    ExactMatch = false;
    //}
}
