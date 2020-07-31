// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Management.Census.Trace;
using System;

namespace Steeltoe.Management.Tracing
{
    public class TracingOptions : ITracingOptions
    {
        internal const string CONFIG_PREFIX = "management:tracing";
        internal const string SPRING_APPLICATION_PREFIX = "spring:application";
        internal const string DEFAULT_INGRESS_IGNORE_PATTERN = "/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|/hystrix.stream|.*\\.gif";
        internal const string DEFAULT_EGRESS_IGNORE_PATTERN = "/api/v2/spans|/v2/apps/.*/permissions";

        public TracingOptions(string defaultName, IConfiguration config)
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

            if (string.IsNullOrEmpty(Name))
            {
                Name = GetApplicationName(defaultName, config);
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

        internal TracingOptions()
        {
        }

        public string Name { get; set; }

        public string IngressIgnorePattern { get; set; }

        public string EgressIgnorePattern { get; set; }

        public int MaxNumberOfAttributes { get; set; }

        public int MaxNumberOfAnnotations { get; set; }

        public int MaxNumberOfMessageEvents { get; set; }

        public int MaxNumberOfLinks { get; set; }

        public bool AlwaysSample { get; set; }

        public bool NeverSample { get; set; }

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
