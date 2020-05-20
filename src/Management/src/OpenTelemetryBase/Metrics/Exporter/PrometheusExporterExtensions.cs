// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
