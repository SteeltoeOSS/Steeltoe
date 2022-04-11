// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Exporters;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.OpenTelemetry.Metrics
{
    public static class MeterProviderBuilderExtensions
    {
        public static MeterProviderBuilder AddRegisteredViews(this MeterProviderBuilder builder, IViewRegistry viewRegistry)
        {
            if (viewRegistry != null)
            {
                foreach (var view in viewRegistry.Views)
                {
                    builder.AddView(view.Key, view.Value);
                }
            }

            return builder;
        }

        public static MeterProviderBuilder AddExporters(this MeterProviderBuilder builder, IEnumerable<IMetricsExporter> exporters)
        {
            var steeltoeExporter = exporters.OfType<SteeltoeExporter>().FirstOrDefault();
            if (steeltoeExporter != null)
            {
                builder = builder.AddSteeltoeExporter(steeltoeExporter);
            }

            var prometheusExporter = exporters.OfType<SteeltoePrometheusExporter>().FirstOrDefault();
            if (prometheusExporter != null)
            {
                builder = builder.AddReader(new BaseExportingMetricReader(prometheusExporter));
            }

            return builder;
        }

        public static MeterProviderBuilder AddWavefrontExporter(this MeterProviderBuilder builder, WavefrontMetricsExporter wavefrontExporter)
        {
            var metricReader = new PeriodicExportingMetricReader(wavefrontExporter, wavefrontExporter.Options.Step);

            metricReader.Temporality = AggregationTemporality.Cumulative;
            return builder.AddReader(metricReader);
        }
    }
}
