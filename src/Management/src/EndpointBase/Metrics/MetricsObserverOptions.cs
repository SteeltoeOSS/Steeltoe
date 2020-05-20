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
using System;
using System.Collections.Generic;
using System.Data;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricsObserverOptions : IMetricsObserverOptions
    {
        internal const string MANAGEMENT_METRICS_PREFIX = "management:metrics:observer";
        internal const string DEFAULT_INGRESS_IGNORE_PATTERN = "/cloudfoundryapplication|/cloudfoundryapplication/.*|.*\\.png|.*\\.css|.*\\.js|.*\\.html|/favicon.ico|/hystrix.stream|.*\\.gif";
        internal const string DEFAULT_EGRESS_IGNORE_PATTERN = "/api/v2/spans|/v2/apps/.*/permissions";

        public MetricsObserverOptions()
            : base()
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

        public string IngressIgnorePattern { get; set; }

        public string EgressIgnorePattern { get; set; }

        public bool AspNetCoreHosting { get; set; } = true;

        public bool GCEvents { get; set; } = true;

        public bool ThreadPoolEvents { get; set; } = true;

        public bool EventCounterEvents { get; set; } = false;

        public bool HttpClientCore { get; set; } = false;

        public bool HttpClientDesktop { get; set; } = false;

        public bool HystrixEvents { get; set; } = false;
    }
}
