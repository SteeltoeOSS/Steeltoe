// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Exporters.Prometheus;
using System;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Steeltoe.Management.OpenTelemetry
{
    public class OpenTelemetryMetrics
    {
        public static readonly AssemblyName AssemblyName = typeof(OpenTelemetryMetrics).Assembly.GetName();
        public static readonly string InstrumentationName = AssemblyName.Name;
        public static readonly string InstrumentationVersion = AssemblyName.Version.ToString();

        public static OpenTelemetryMetrics Instance { get; private set; }

        public static MeterProvider MeterProvider { get; private set; }

        public static Meter Meter => new Meter(InstrumentationName, InstrumentationVersion);
        /*
         *  //var view = View.Create(
            //        ViewName.Create("http.server.request.time"),
            //        "Total request time",
            //        responseTimeMeasure,
            //        Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })),
            //        new List<ITagKey>() { statusTagKey, exceptionTagKey, methodTagKey, uriTagKey });

            //ViewManager.RegisterView(view);

            //view = View.Create(
            //        ViewName.Create("http.server.request.count"),
            //        "Total request counts",
            //        serverCountMeasure,
            //        Sum.Create(),
            //        new List<ITagKey>() { statusTagKey, exceptionTagKey, methodTagKey, uriTagKey });

         */

        public OpenTelemetryMetrics(SteeltoeExporter steeltoeExporter = null, PrometheusExporterWrapper prometheusExporter = null)
        {
            // OpenTelemetry.Metrics.MeterProviderBuilderExtensions.
            var builder = Sdk.CreateMeterProviderBuilder()
                .AddMeter(InstrumentationName, InstrumentationVersion);
               // .AddAspnetServerInstrumentsAndViews() //TODO: Drive with configuration
               // .AddHttpClientInstrumentation();

            if (steeltoeExporter != null)
            {
                builder = builder.AddSteeltoeExporter(steeltoeExporter);
            }

            // .AddPrometheusExporter(configurePrometheus)
            if (prometheusExporter != null)
            {
                builder = builder.AddReader(new BaseExportingMetricReader(prometheusExporter));
            }

            MeterProvider = builder.Build();
        }

        public static void Initialize(SteeltoeExporter steeltoeExporter, PrometheusExporterWrapper prometheusExporter)
        {
            Instance = new OpenTelemetryMetrics(steeltoeExporter, prometheusExporter);
        }
    }
}
