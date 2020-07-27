// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenCensus.Stats;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public class SpringBootMetricWriter : CloudFoundryMetricWriter
    {
        public SpringBootMetricWriter(CloudFoundryForwarderOptions options, IStats stats, ILogger logger = null)
            : base(options, stats, logger)
        {
        }

        public override IList<Metric> CreateMetrics(IViewData viewData, IAggregationData aggregation, TagValues tagValues, long timeStamp)
        {
            var results = new List<Metric>();

            var unit = viewData.View.Measure.Unit;
            var tags = GetTagKeysAndValues(viewData.View.Columns, tagValues.Values);
            var name = viewData.View.Name.AsString;

            aggregation.Match<object>(
                (arg) =>
                {
                    results.Add(new Metric(GetMetricName(name, string.Empty, tags), MetricType.GAUGE, timeStamp, unit, tags, arg.Sum));
                    return null;
                },
                (arg) =>
                {
                    results.Add(new Metric(GetMetricName(name, string.Empty, tags), MetricType.GAUGE, timeStamp, unit, tags, arg.Sum));
                    return null;
                },
                (arg) =>
                {
                    results.Add(new Metric(GetMetricName(name, string.Empty, tags), MetricType.GAUGE, timeStamp, unit, tags, arg.Count));
                    return null;
                },
                (arg) =>
                {
                    results.Add(new Metric(GetMetricName(name, string.Empty, tags), MetricType.GAUGE, timeStamp, unit, tags, arg.Mean));
                    return null;
                },
                (arg) =>
                {
                    results.Add(new Metric(GetMetricName(name, string.Empty, tags), MetricType.GAUGE, timeStamp, unit, tags, arg.Mean));
                    results.Add(new Metric(GetMetricName(name, "max", tags), MetricType.GAUGE, timeStamp, unit, tags, arg.Max));
                    results.Add(new Metric(GetMetricName(name, "min", tags), MetricType.GAUGE, timeStamp, unit, tags, arg.Min));
                    var stdDeviation = Math.Sqrt((arg.SumOfSquaredDeviations / arg.Count) - 1);
                    if (double.IsNaN(stdDeviation))
                    {
                        stdDeviation = 0.0;
                    }

                    results.Add(new Metric(GetMetricName(name, "stddev", tags), MetricType.GAUGE, timeStamp, unit, tags, stdDeviation));
                    return null;
                },
                (arg) =>
                {
                    results.Add(new Metric(GetMetricName(name, "value", tags), MetricType.GAUGE, timeStamp, unit, tags, arg.LastValue));
                    return null;
                },
                (arg) =>
                {
                    results.Add(new Metric(GetMetricName(name, "value", tags), MetricType.GAUGE, timeStamp, unit, tags, arg.LastValue));
                    return null;
                },
                (arg) =>
                {
                    return null;
                });

            return results;
        }

        protected internal string GetMetricName(string viewName, string suffix, IDictionary<string, string> tags)
        {
            var metricName = string.IsNullOrEmpty(suffix) ? viewName : viewName + "." + suffix;
            foreach (var key in tags.Keys)
            {
                if (!ShouldSkipTag(key))
                {
                    var tagValue = GetTagValue(tags[key]);
                    if (!tagValue.StartsWith("."))
                    {
                        metricName = metricName + "." + tagValue;
                    }
                    else
                    {
                        metricName += tagValue;
                    }
                }
            }

            return metricName;
        }

        protected internal bool ShouldSkipTag(string tagKey)
        {
            if (tagKey == "clientName")
            {
                return true;
            }

            if (tagKey == "exception")
            {
                return true;
            }

            return false;
        }

        protected internal string GetTagValue(string tag)
        {
            if (tag == "/")
            {
                return "root";
            }

            return tag.Replace('/', '.');
        }
    }
}