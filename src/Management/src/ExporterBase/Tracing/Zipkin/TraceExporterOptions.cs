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
using System;

namespace Steeltoe.Management.Exporter.Tracing.Zipkin
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
