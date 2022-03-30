using OpenTelemetry;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.OpenTelemetry.Exporters.Wavefront
{
    public static class WavefrontTraceExtensions
    {
        public static TracerProviderBuilder AddWavefrontExporter(this TracerProviderBuilder builder, IWavefrontExporterOptions exporterOptions)
        {
            var options = exporterOptions as WavefrontExporterOptions ?? throw new ArgumentNullException(nameof(exporterOptions));
            var exporter = new WavefrontTraceExporter(exporterOptions, null);

            return builder.AddProcessor(new BatchActivityExportProcessor(exporter, options.MaxQueueSize, options.Step));
        }
    }
}
