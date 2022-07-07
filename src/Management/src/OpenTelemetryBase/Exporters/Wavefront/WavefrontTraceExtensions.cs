// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System;

namespace Steeltoe.Management.OpenTelemetry.Exporters.Wavefront;

public static class WavefrontTraceExtensions
{
    public static TracerProviderBuilder AddWavefrontExporter(this TracerProviderBuilder builder, IWavefrontExporterOptions exporterOptions, ILogger<WavefrontTraceExporter> logger = null)
    {
        var options = exporterOptions as WavefrontExporterOptions ?? throw new ArgumentNullException(nameof(exporterOptions));
        var exporter = new WavefrontTraceExporter(exporterOptions, logger);

        return builder.AddProcessor(new BatchActivityExportProcessor(exporter, options.MaxQueueSize, options.Step));
    }
}
