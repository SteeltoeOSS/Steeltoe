// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Net;
using System;

namespace Steeltoe.Management.OpenTelemetry.Exporters.Wavefront
{
    public class WavefrontExporterOptions : IWavefrontExporterOptions
    {
        // Note: this key is shared between tracing and metrics to mirror the Spring boot configuration settings.
        public const string WAVEFRONT_PREFIX = "management:metrics:export:wavefront";

        public WavefrontExporterOptions(IConfiguration config)
        {
            var section = config?.GetSection(WAVEFRONT_PREFIX) ?? throw new ArgumentNullException(nameof(config));
            section.Bind(this);
            ApplicationOptions = new WavefrontApplicationOptions(config);
        }

        public string Uri { get; set; }

        public string ApiToken { get; set; }

        public int Step { get; set; } = 30000; // milliseconds

        public int BatchSize { get; set; } = 10000;

        public int MaxQueueSize { get; set; } = 1000;

        public WavefrontApplicationOptions ApplicationOptions { get; }

        public string Source => ApplicationOptions?.Source ?? DnsTools.ResolveHostName();

        public string Name => ApplicationOptions?.Name ?? "SteeltoeApp";

        public string Service => ApplicationOptions?.Service ?? "SteeltoeAppservice";

        public string Cluster { get; set; }
    }
}
