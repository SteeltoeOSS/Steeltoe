// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Net;
using Steeltoe.Management.OpenTelemetry.Exporters;
using System;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class WavefrontExporterOptions
    {
        internal const string MANAGEMENT_PREFIX = "management:metrics:export:wavefront";

        public WavefrontExporterOptions()
        {
        }

        public WavefrontExporterOptions(IConfiguration config)
        {
            var section = config?.GetSection(MANAGEMENT_PREFIX) ?? throw new ArgumentNullException(nameof(config));
            section.Bind(this);
            ApplicationOptions = new WavefrontApplicationOptions(config);
        }

        public string Uri { get; set; }

        public string ApiToken { get; set; }

        public int Step { get; set; } = 30000; // milliseconds

        public int BatchSize { get; set; } = 10000;

        public int MaxQueueSize { get; set; } = 1000;

        public WavefrontApplicationOptions ApplicationOptions { get; }

        public WavefrontConfig Config => new WavefrontConfig()
            {
                AppName = ApplicationOptions?.Name,
                Service = ApplicationOptions?.Service ?? ApplicationOptions?.Name,
                Cluster = ApplicationOptions?.Cluster,
                Source = ApplicationOptions?.Source ?? DnsTools.ResolveHostName(),

                WavefrontURL = Uri,
                Token = ApiToken,
                BatchSize = BatchSize,
                Step = Step,
                MaxQueueSize = MaxQueueSize
            };
    }
}
