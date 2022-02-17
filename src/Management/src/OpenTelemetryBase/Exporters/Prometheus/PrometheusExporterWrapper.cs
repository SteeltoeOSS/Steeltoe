using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;
using System;

namespace Steeltoe.Management.OpenTelemetry.Exporters.Prometheus
{
    public class PrometheusExporterWrapper : BaseExporter<Metric>, IPullMetricExporter
    {
      //  private readonly PrometheusExporter _exporter;

        //public PrometheusExporterOptions Options { get; internal set; }

        internal PrometheusCollectionManager CollectionManager { get; }

        public PrometheusExporterWrapper()
        {
          //  Options = options;
           // _exporter = new PrometheusExporter(options);
           // Collect = _exporter.Collect;
            CollectionManager = new PrometheusCollectionManager(this);
        }

        public Func<int, bool> Collect
        {
            get;
            set;
        }

        //public ExportResult Export(in Batch<Metric> metrics)
        //{
        //    return OnExport(metrics);
        //}

        public override ExportResult Export(in Batch<Metric> batch)
        {
            return OnExport(batch);
        }

        internal Func<Batch<Metric>, ExportResult> OnExport
        {
            get; set;
        }

    }
}
