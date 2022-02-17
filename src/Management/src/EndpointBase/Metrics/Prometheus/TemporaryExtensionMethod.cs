using OpenTelemetry;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint.Metrics.Prometheus
{
    public static class TemporaryExtensionMethod
    {
        //public static bool TryFindExporter<T>(this MeterProvider provider, out T exporter)
        //   where T : BaseExporter<Metric>
        //{
        //    var type = provider.GetType();
        //    if (type.ToString().Contains("MeterProviderSdk"))
        //    {
        //        var propInfo = type.GetProperty("Reader", System.Reflection.BindingFlags.Public);
        //        var reader = propInfo.GetValue(provider) as MetricReader;
        //        return TryFindExporter(reader, out exporter);
        //    }

        //    exporter = null;
        //    return false;

        //    static bool TryFindExporter(MetricReader reader, out T exporter)
        //    {
        //        if (reader is BaseExportingMetricReader exportingMetricReader)
        //        {
        //            var type = exportingMetricReader.GetType();
        //            var propInfo = type.GetProperty("Exporter", System.Reflection.BindingFlags.Public);
        //            exporter = propInfo.GetValue(exportingMetricReader) as T;
        //            // exporter = exportingMetricReader.Exporter as T;
        //            return exporter != null;
        //        }

        //        //if (reader is CompositeMetricReader compositeMetricReader)
        //        //{
        //        //    foreach (MetricReader childReader in compositeMetricReader)
        //        //    {
        //        //        if (TryFindExporter(childReader, out exporter))
        //        //        {
        //        //            return true;
        //        //        }
        //        //    }
        //        //}

        //        exporter = null;
        //        return false;
        //    }
        //}
    }
}
