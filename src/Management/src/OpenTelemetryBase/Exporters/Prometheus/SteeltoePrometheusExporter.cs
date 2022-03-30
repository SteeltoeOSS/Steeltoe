// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry;
using OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Exporters.Prometheus;
using System;

namespace Steeltoe.Management.OpenTelemetry.Exporters
{
    public class SteeltoePrometheusExporter : IMetricsExporter
    {
        internal PullmetricsCollectionManager CollectionManager { get; }

        internal long ScrapeResponseCacheDurationMilliseconds { get; }

        private byte[] _buffer = new byte[85000]; // encourage the object to live in LOH (large object heap)

        internal SteeltoePrometheusExporter(IPullmetricsExporterOptions options = null)
        {
            ScrapeResponseCacheDurationMilliseconds = options?.ScrapeResponseCacheDurationMilliseconds ?? 5000;
            CollectionManager = new PullmetricsCollectionManager(this);
        }

        public override Func<int, bool> Collect
        {
            get;
            set;
        }

        public override ExportResult Export(in Batch<Metric> batch)
        {
            var result = ExportResult.Failure;
            if (OnExport != null)
            {
                result = OnExport(batch);
            }

            return result;
        }

        internal override Func<Batch<Metric>, ExportResult> OnExport
        {
            get; set;
        }

        internal override ICollectionResponse GetCollectionResponse(Batch<Metric> metrics = default)
        {
            int cursor = 0;

            foreach (var metric in metrics)
            {
                while (true)
                {
                    try
                    {
                        cursor = PrometheusSerializer.WriteMetric(_buffer, cursor, metric);
                        break;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        int bufferSize = _buffer.Length * 2;

                        // there are two cases we might run into the following condition:
                        // 1. we have many metrics to be exported - in this case we probably want
                        //    to put some upper limit and allow the user to configure it.
                        // 2. we got an IndexOutOfRangeException which was triggered by some other
                        //    code instead of the buffer[cursor++] - in this case we should give up
                        //    at certain point rather than allocating like crazy.
                        if (bufferSize > 100 * 1024 * 1024)
                        {
                            throw;
                        }

                        var newBuffer = new byte[bufferSize];
                        _buffer.CopyTo(newBuffer, 0);
                        _buffer = newBuffer;
                    }
                }
            }

            var dataview = new ArraySegment<byte>(_buffer, 0, Math.Max(cursor - 1, 0));
            return new PrometheusCollectionResponse(dataview, DateTime.Now);
        }

        internal override ICollectionResponse GetCollectionResponse(ICollectionResponse collectionResponse, DateTime updatedTime)
        {
            var prometheusResponse = (PrometheusCollectionResponse)collectionResponse;
            return new PrometheusCollectionResponse(prometheusResponse.View, updatedTime);
        }
    }
}
