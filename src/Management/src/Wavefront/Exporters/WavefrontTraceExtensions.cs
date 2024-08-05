// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Steeltoe.Management.Wavefront.Exporters;

public static class WavefrontTraceExtensions
{
    public static TracerProviderBuilder AddWavefrontTraceExporter(this TracerProviderBuilder builder, WavefrontExporterOptions options,
        ILogger<WavefrontTraceExporter> logger)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var exporter = new WavefrontTraceExporter(options, logger);

        return builder.AddProcessor(new BatchActivityExportProcessor(exporter, options.MaxQueueSize, options.Step));
    }
}
