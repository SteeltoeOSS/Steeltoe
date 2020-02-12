// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
