// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using System;

namespace Steeltoe.Management.OpenTelemetry.Trace.Exporter.Zipkin
{
    public class TraceExporterOptions : ITraceExporterOptions
    {
        internal const int DEFAULT_TIMEOUT = 3;
        internal const string DEFAULT_ENDPOINT = "http://localhost:9411/api/v2/spans";

        private const string CONFIG_PREFIX = "management:tracing:exporter:zipkin";
        private IApplicationInstanceInfo applicationInstanceInfo;

        public TraceExporterOptions()
        {
            Endpoint = DEFAULT_ENDPOINT;
        }

        public TraceExporterOptions(IApplicationInstanceInfo appInfo, IConfiguration config)
            : this()
        {
            if (appInfo is null)
            {
                throw new ArgumentNullException(nameof(appInfo));
            }

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
            ServiceName ??= applicationInstanceInfo.ApplicationNameInContext(SteeltoeComponent.Management, CONFIG_PREFIX + ":serviceName");
        }

        public string Endpoint { get; set; }

        public bool ValidateCertificates { get; set; } = true;

        public int TimeoutSeconds { get; set; } = DEFAULT_TIMEOUT;

        public string ServiceName { get; set; }

        public bool UseShortTraceIds { get; set; } = true;
    }
}
