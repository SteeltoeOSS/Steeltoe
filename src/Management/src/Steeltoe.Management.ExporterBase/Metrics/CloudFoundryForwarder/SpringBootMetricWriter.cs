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
            List<Metric> results = new List<Metric>();

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
            string metricName = string.IsNullOrEmpty(suffix) ? viewName : viewName + "." + suffix;
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
                        metricName = metricName + tagValue;
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