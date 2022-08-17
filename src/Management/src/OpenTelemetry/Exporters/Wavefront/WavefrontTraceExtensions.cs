// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Steeltoe.Common;

namespace Steeltoe.Management.OpenTelemetry.Exporters.Wavefront;

public static class WavefrontTraceExtensions
{
    public static TracerProviderBuilder AddWavefrontExporter(this TracerProviderBuilder builder, IWavefrontExporterOptions options,
        ILogger<WavefrontTraceExporter> logger = null)
    {
        ArgumentGuard.NotNull(options);

        if (options is not WavefrontExporterOptions exporterOptions)
        {
            throw new ArgumentException($"Options must be convertible to {nameof(WavefrontExporterOptions)}.", nameof(options));
        }

        var exporter = new WavefrontTraceExporter(exporterOptions, logger);

        return builder.AddProcessor(new BatchActivityExportProcessor(exporter, exporterOptions.MaxQueueSize, exporterOptions.Step));
    }
}
