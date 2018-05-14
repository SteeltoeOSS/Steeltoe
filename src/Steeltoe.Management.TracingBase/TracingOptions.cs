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

using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
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

        public bool UseShortTraceIds { get; set; }

        internal string GetApplicationName(string defaultName, IConfiguration config)
        {
            var section = config.GetSection(CloudFoundryApplicationOptions.CONFIGURATION_PREFIX);
            if (section != null)
            {
                CloudFoundryApplicationOptions appOptions = new CloudFoundryApplicationOptions(section);
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
