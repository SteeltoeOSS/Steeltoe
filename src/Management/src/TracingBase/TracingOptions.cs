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
        internal const string DEFAULT_INGRESS_IGNORE_PATTERN = "/actuator/.*|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|/hystrix.stream|.*\\.gif";
        internal const string DEFAULT_EGRESS_IGNORE_PATTERN = "/api/v2/spans|/v2/apps/.*/permissions|/eureka/*";
        private readonly IApplicationInstanceInfo _applicationInstanceInfo;

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

            _applicationInstanceInfo = appInfo;

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

        /// <inheritdoc />
        public string Name => _applicationInstanceInfo?.ApplicationNameInContext(SteeltoeComponent.Management, CONFIG_PREFIX + ":name");

        /// <inheritdoc />
        public string IngressIgnorePattern { get; set; }

        /// <inheritdoc />
        public string EgressIgnorePattern { get; set; }

        /// <inheritdoc />
        public bool AlwaysSample { get; set; }

        /// <inheritdoc />
        public bool NeverSample { get; set; }

        /// <inheritdoc />
        public bool UseShortTraceIds { get; set; }

        /// <inheritdoc />
        public string PropagationType { get; set; } = "B3";

        /// <inheritdoc />
        public bool SingleB3Header { get; set; } = true;

        /// <inheritdoc />
        public bool EnableGrpcAspNetCoreSupport { get; set; } = true;
    }
}
