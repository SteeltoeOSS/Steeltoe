// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;

namespace Steeltoe.Management.Exporter.Tracing.Zipkin
{
    public class TraceExporterOptions : ITraceExporterOptions
    {
        internal const int DEFAULT_TIMEOUT = 3;
        internal const string DEFAULT_ENDPOINT = "http://localhost:9411/api/v2/spans";

        private const string CONFIG_PREFIX = "management:tracing:exporter:zipkin";
        private const string SPRING_APPLICATION_PREFIX = "spring:application";

        public TraceExporterOptions()
        {
            Endpoint = DEFAULT_ENDPOINT;
        }

        public TraceExporterOptions(string defaultServiceName, IConfiguration config)
            : this()
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

            if (string.IsNullOrEmpty(ServiceName))
            {
                ServiceName = GetApplicationName(defaultServiceName, config);
            }
        }

        public string Endpoint { get; set; }

        public bool ValidateCertificates { get; set; } = true;

        public int TimeoutSeconds { get; set; } = DEFAULT_TIMEOUT;

        public string ServiceName { get; set; }

        public bool UseShortTraceIds { get; set; } = true;

        internal string GetApplicationName(string defaultName, IConfiguration config)
        {
            var section = config.GetSection(CloudFoundryApplicationOptions.CONFIGURATION_PREFIX);
            if (section != null)
            {
                var appOptions = new CloudFoundryApplicationOptions(section);
                if (!string.IsNullOrEmpty(appOptions.Name))
                {
                    return appOptions.Name;
                }
            }

            section = config.GetSection(SPRING_APPLICATION_PREFIX);
            if (section != null)
            {
                var name = section["name"];
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            if (!string.IsNullOrEmpty(defaultName))
            {
                return defaultName;
            }

            return "Unknown";
        }
    }
}
