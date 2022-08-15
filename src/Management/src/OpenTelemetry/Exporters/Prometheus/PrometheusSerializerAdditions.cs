// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Steeltoe.Management.OpenTelemetry.Exporters.Prometheus;

/// <summary>
/// OpenTelemetry additions to the PrometheusSerializer.
/// </summary>
internal static class PrometheusSerializerAdditions
{
    private static readonly string[] MetricTypes =
    {
        "untyped",
        "counter",
        "gauge",
        "summary",
        "histogram",
        "histogram",
        "histogram",
        "histogram",
        "untyped"
    };

    public static int WriteMetric(byte[] buffer, int cursor, Metric metric)
    {
        if (!string.IsNullOrWhiteSpace(metric.Description))
        {
            cursor = PrometheusSerializer.WriteHelpText(buffer, cursor, metric.Name, metric.Unit, metric.Description);
        }

        int metricType = (int)metric.MetricType >> 4;
        cursor = PrometheusSerializer.WriteTypeInfo(buffer, cursor, metric.Name, metric.Unit, MetricTypes[metricType]);

        if (!metric.MetricType.IsHistogram())
        {
            foreach (ref readonly MetricPoint metricPoint in metric.GetMetricPoints())
            {
                ReadOnlyTagCollection tags = metricPoint.Tags;
                long timestamp = metricPoint.EndTime.ToUnixTimeMilliseconds();

                // Counter and Gauge
                cursor = PrometheusSerializer.WriteMetricName(buffer, cursor, metric.Name, metric.Unit);

                if (tags.Count > 0)
                {
                    buffer[cursor++] = unchecked((byte)'{');

                    foreach (KeyValuePair<string, object> tag in tags)
                    {
                        cursor = PrometheusSerializer.WriteLabel(buffer, cursor, tag.Key, tag.Value);
                        buffer[cursor++] = unchecked((byte)',');
                    }

                    buffer[cursor - 1] = unchecked((byte)'}'); // Note: We write the '}' over the last written comma, which is extra.
                }

                buffer[cursor++] = unchecked((byte)' ');

                // TODO: MetricType is same for all MetricPoints
                // within a given Metric, so this check can avoided
                // for each MetricPoint
                if (((int)metric.MetricType & 0b_0000_1111) == 0x0a /* I8 */)
                {
                    cursor = PrometheusSerializer.WriteLong(buffer, cursor,
                        metric.MetricType.IsSum() ? metricPoint.GetSumLong() : metricPoint.GetGaugeLastValueLong());
                }
                else
                {
                    cursor = PrometheusSerializer.WriteDouble(buffer, cursor,
                        metric.MetricType.IsSum() ? metricPoint.GetSumDouble() : metricPoint.GetGaugeLastValueDouble());
                }

                buffer[cursor++] = unchecked((byte)' ');

                cursor = PrometheusSerializer.WriteLong(buffer, cursor, timestamp);

                buffer[cursor++] = PrometheusSerializer.AsciiLinefeed;
            }
        }
        else
        {
            foreach (ref readonly MetricPoint metricPoint in metric.GetMetricPoints())
            {
                ReadOnlyTagCollection tags = metricPoint.Tags;
                long timestamp = metricPoint.EndTime.ToUnixTimeMilliseconds();

                long totalCount = 0;

                foreach (HistogramBucket histogramMeasurement in metricPoint.GetHistogramBuckets())
                {
                    totalCount += histogramMeasurement.BucketCount;

                    cursor = PrometheusSerializer.WriteMetricName(buffer, cursor, metric.Name, metric.Unit);
                    cursor = PrometheusSerializer.WriteAsciiStringNoEscape(buffer, cursor, "_bucket{");

                    foreach (KeyValuePair<string, object> tag in tags)
                    {
                        cursor = PrometheusSerializer.WriteLabel(buffer, cursor, tag.Key, tag.Value);
                        buffer[cursor++] = unchecked((byte)',');
                    }

                    cursor = PrometheusSerializer.WriteAsciiStringNoEscape(buffer, cursor, "le=\"");

                    cursor = !double.IsPositiveInfinity(histogramMeasurement.ExplicitBound)
                        ? PrometheusSerializer.WriteDouble(buffer, cursor, histogramMeasurement.ExplicitBound)
                        : PrometheusSerializer.WriteAsciiStringNoEscape(buffer, cursor, "+Inf");

                    cursor = PrometheusSerializer.WriteAsciiStringNoEscape(buffer, cursor, "\"} ");

                    cursor = PrometheusSerializer.WriteLong(buffer, cursor, totalCount);
                    buffer[cursor++] = unchecked((byte)' ');

                    cursor = PrometheusSerializer.WriteLong(buffer, cursor, timestamp);

                    buffer[cursor++] = PrometheusSerializer.AsciiLinefeed;
                }

                // Histogram sum
                cursor = PrometheusSerializer.WriteMetricName(buffer, cursor, metric.Name, metric.Unit);
                cursor = PrometheusSerializer.WriteAsciiStringNoEscape(buffer, cursor, "_sum");

                if (tags.Count > 0)
                {
                    buffer[cursor++] = unchecked((byte)'{');

                    foreach (KeyValuePair<string, object> tag in tags)
                    {
                        cursor = PrometheusSerializer.WriteLabel(buffer, cursor, tag.Key, tag.Value);
                        buffer[cursor++] = unchecked((byte)',');
                    }

                    buffer[cursor - 1] = unchecked((byte)'}'); // Note: We write the '}' over the last written comma, which is extra.
                }

                buffer[cursor++] = unchecked((byte)' ');

                cursor = PrometheusSerializer.WriteDouble(buffer, cursor, metricPoint.GetHistogramSum());
                buffer[cursor++] = unchecked((byte)' ');

                cursor = PrometheusSerializer.WriteLong(buffer, cursor, timestamp);

                buffer[cursor++] = PrometheusSerializer.AsciiLinefeed;

                // Histogram count
                cursor = PrometheusSerializer.WriteMetricName(buffer, cursor, metric.Name, metric.Unit);
                cursor = PrometheusSerializer.WriteAsciiStringNoEscape(buffer, cursor, "_count");

                if (tags.Count > 0)
                {
                    buffer[cursor++] = unchecked((byte)'{');

                    foreach (KeyValuePair<string, object> tag in tags)
                    {
                        cursor = PrometheusSerializer.WriteLabel(buffer, cursor, tag.Key, tag.Value);
                        buffer[cursor++] = unchecked((byte)',');
                    }

                    buffer[cursor - 1] = unchecked((byte)'}'); // Note: We write the '}' over the last written comma, which is extra.
                }

                buffer[cursor++] = unchecked((byte)' ');

                cursor = PrometheusSerializer.WriteLong(buffer, cursor, metricPoint.GetHistogramCount());
                buffer[cursor++] = unchecked((byte)' ');

                cursor = PrometheusSerializer.WriteLong(buffer, cursor, timestamp);

                buffer[cursor++] = PrometheusSerializer.AsciiLinefeed;
            }
        }

        buffer[cursor++] = PrometheusSerializer.AsciiLinefeed;

        return cursor;
    }
}
