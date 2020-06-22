﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics.Export;
using System.IO;
using System.Text;

namespace Steeltoe.Management.OpenTelemetry.Metrics.Exporter
{
    /// <summary>
    /// Helper to write metrics collection from exporter in Prometheus format.
    /// </summary>
    public static class PrometheusExporterExtensions
    {
        /// <summary>
        /// Serialize to Prometheus Format.
        /// </summary>
        /// <param name="exporter">Prometheus Exporter.</param>
        /// <param name="writer">StreamWriter to write to.</param>
        public static void WriteMetricsCollection(this PrometheusExporter exporter, StreamWriter writer)
        {
            foreach (var metric in exporter.GetAndClearDoubleMetrics())
            {
                var labels = metric.Labels;
                var builder = new PrometheusMetricBuilder()
                    .WithName(metric.MetricName)
                    .WithDescription(metric.MetricDescription);

                switch (metric.AggregationType)
                {
                    case AggregationType.DoubleSum:
                        {
                            var doubleSum = metric.Data as SumData<double>;
                            var doubleValue = doubleSum.Sum;

                            builder = builder.WithType("counter");
                            var metricValueBuilder = builder.AddValue();
                            metricValueBuilder = metricValueBuilder.WithValue(doubleValue);

                            foreach (var label in labels)
                            {
                                metricValueBuilder.WithLabel(label.Key, label.Value);
                            }

                            builder.Write(writer);
                            break;
                        }

                    case AggregationType.Summary:
                        {
                            var doubleSummary = metric.Data as SummaryData<double>;

                            builder = builder.WithType("summary");
                            var metricValueBuilder = builder.AddValue();
                            var mean = 0D;

                            if (doubleSummary.Count > 0)
                            {
                                mean = doubleSummary.Sum / doubleSummary.Count;
                            }

                            metricValueBuilder = metricValueBuilder.WithValue(mean);

                            foreach (var label in labels)
                            {
                                metricValueBuilder.WithLabel(label.Key, label.Value);
                            }

                            builder.Write(writer);
                            break;
                        }
                }
            }

            foreach (var metric in exporter.GetAndClearLongMetrics())
            {
                var labels = metric.Labels;
                var builder = new PrometheusMetricBuilder()
                    .WithName(metric.MetricName)
                    .WithDescription(metric.MetricDescription);

                switch (metric.AggregationType)
                {
                    case AggregationType.LongSum:
                        {
                            var longSum = metric.Data as SumData<long>;
                            var longValue = longSum.Sum;
                            builder = builder.WithType("counter");

                            foreach (var label in labels)
                            {
                                var metricValueBuilder = builder.AddValue();
                                metricValueBuilder = metricValueBuilder.WithValue(longValue);
                                metricValueBuilder.WithLabel(label.Key, label.Value);
                            }

                            builder.Write(writer);
                            break;
                        }

                    case AggregationType.Summary:
                        {
                            var longSummary = metric.Data as SummaryData<long>;

                            builder = builder.WithType("summary");
                            var metricValueBuilder = builder.AddValue();
                            var mean = 0L;

                            if (longSummary.Count > 0)
                            {
                                mean = longSummary.Sum / longSummary.Count;
                            }

                            metricValueBuilder = metricValueBuilder.WithValue(mean);

                            foreach (var label in labels)
                            {
                                metricValueBuilder.WithLabel(label.Key, label.Value);
                            }

                            builder.Write(writer);
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Get Metrics Collection as a string.
        /// </summary>
        /// <param name="exporter"> Prometheus Exporter. </param>
        /// <returns>Metrics serialized to string in Prometheus format.</returns>
        public static string GetMetricsCollection(this PrometheusExporter exporter)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            WriteMetricsCollection(exporter, writer);
            writer.Flush();

            var str = Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Length);
            return str;
        }
    }
}
