// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricsObserverOptions : IMetricsObserverOptions
    {
        internal const string MANAGEMENT_METRICS_PREFIX = "management:metrics:observer";
        internal const string DEFAULT_INGRESS_IGNORE_PATTERN = "/cloudfoundryapplication|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|/hystrix.stream|.*\\.gif";
        internal const string DEFAULT_EGRESS_IGNORE_PATTERN = "/api/v2/spans|/v2/apps/.*/permissions";

        public MetricsObserverOptions()
        {
            IngressIgnorePattern = DEFAULT_INGRESS_IGNORE_PATTERN;
            EgressIgnorePattern = DEFAULT_EGRESS_IGNORE_PATTERN;
        }

        public MetricsObserverOptions(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(MANAGEMENT_METRICS_PREFIX);
            if (section != null)
            {
                section.Bind(this);
            }

            if (string.IsNullOrEmpty(IngressIgnorePattern))
            {
                IngressIgnorePattern = DEFAULT_INGRESS_IGNORE_PATTERN;
            }

            if (string.IsNullOrEmpty(EgressIgnorePattern))
            {
                EgressIgnorePattern = DEFAULT_EGRESS_IGNORE_PATTERN;
            }
        }

        /// <inheritdoc/>
        public string IngressIgnorePattern { get; set; }

        /// <inheritdoc/>
        public string EgressIgnorePattern { get; set; }

        public bool AspNetCoreHosting { get; set; } = true;

        public bool GCEvents { get; set; } = true;

        public bool ThreadPoolEvents { get; set; } = true;

        public bool EventCounterEvents { get; set; } = false;

        public bool HttpClientCore { get; set; } = false;

        public bool HttpClientDesktop { get; set; } = false;

        public bool HystrixEvents { get; set; } = false;

        /// <inheritdoc/>
        public List<string> ExcludedMetrics { get; set; } = new ();
    }
}
