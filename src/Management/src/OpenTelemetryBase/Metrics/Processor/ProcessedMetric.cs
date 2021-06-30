// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics.Export;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.OpenTelemetry.Metrics.Processor
{
    [Obsolete("Steeltoe uses the OpenTelemetry Metrics API, which is not considered stable yet, see https://github.com/SteeltoeOSS/Steeltoe/issues/711 more information")]
    public class ProcessedMetric<T> : Metric<T>
    {
        public ProcessedMetric(
            string metricNamespace,
            string metricName,
            string desc,
            IEnumerable<KeyValuePair<string, string>> labels,
            AggregationType type)
            : base(metricNamespace, metricName, desc, labels, type)
        {
        }

        public ProcessedMetric(ProcessedMetric<T> metric)
            : base(metric.MetricNamespace, metric.MetricName, metric.MetricDescription, metric.Labels, metric.AggregationType)
        {
        }

        public new MetricData<T> Data { get; set; }
    }
}
