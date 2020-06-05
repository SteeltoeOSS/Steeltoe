// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Trace;
using System;

namespace Steeltoe.Management.Tracing
{
    public class TracingOptions : ITracingOptions
    {
        internal const string CONFIG_PREFIX = "management:tracing";
        internal const string DEFAULT_INGRESS_IGNORE_PATTERN = "/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|/hystrix.stream|.*\\.gif";
        internal const string DEFAULT_EGRESS_IGNORE_PATTERN = "/api/v2/spans|/v2/apps/.*/permissions";
        private IApplicationInstanceInfo applicationInstanceInfo;

        public TracingOptions(IApplicationInstanceInfo appInfo, IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var section = config.GetSection(CONFIG_PREFIX);
            if (section != null)
            {
                section.Bind(this);
            }

            applicationInstanceInfo = appInfo;

            if (string.IsNullOrEmpty(IngressIgnorePattern))
            {
                IngressIgnorePattern = DEFAULT_INGRESS_IGNORE_PATTERN;
            }

            if (string.IsNullOrEmpty(EgressIgnorePattern))
            {
                EgressIgnorePattern = DEFAULT_EGRESS_IGNORE_PATTERN;
            }
        }

        internal TracingOptions()
        {
        }

        public string Name => applicationInstanceInfo?.ApplicationNameInContext(SteeltoeComponent.Management, CONFIG_PREFIX + ":name");

        public string IngressIgnorePattern { get; set; }

        public string EgressIgnorePattern { get; set; }

        public int MaxNumberOfAttributes { get; set; }

        public int MaxNumberOfAnnotations { get; set; }

        public int MaxNumberOfMessageEvents { get; set; }

        public int MaxNumberOfLinks { get; set; }

        public bool AlwaysSample { get; set; }

        public bool NeverSample { get; set; }

        public bool UseShortTraceIds { get; set; } = true;
    }
}
