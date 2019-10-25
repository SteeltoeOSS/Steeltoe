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
using System.Collections.Generic;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public class MicrometerMetricWriter : CloudFoundryMetricWriter
    {
        public MicrometerMetricWriter(CloudFoundryForwarderOptions options, IStats stats, ILogger logger = null)
            : base(options, stats, logger)
        {
        }

        public override IList<Metric> CreateMetrics(IViewData viewData, IAggregationData aggregation, TagValues tagValues, long timeStamp)
        {
            List<Metric> results = new List<Metric>();

            var unit = viewData.View.Measure.Unit;
            var name = viewData.View.Name.AsString;
            var tags = GetTagKeysAndValues(viewData.View.Columns, tagValues.Values);
            var statistic = GetStatistic(viewData.View.Aggregation, viewData.View.Measure);

            aggregation.Match<object>(
                (arg) =>
                {
                    if (statistic == "unknown")
                    {
                        statistic = "total";
                    }

                    tags["statistic"] = statistic;
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, unit, tags, arg.Sum));
                    return null;
                },
                (arg) =>
                {
                    if (statistic == "unknown")
                    {
                        statistic = "total";
                    }

                    tags["statistic"] = statistic;
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, unit, tags, arg.Sum));
                    return null;
                },
                (arg) =>
                {
                    if (statistic == "unknown")
                    {
                        statistic = "count";
                    }

                    tags["statistic"] = statistic;
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, unit, tags, arg.Count));
                    return null;
                },
                (arg) =>
                {
                    if (statistic == "unknown")
                    {
                        statistic = "total";
                    }

                    IDictionary<string, string> copy = new Dictionary<string, string>(tags);
                    copy["statistic"] = "count";
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, "count", copy, arg.Count));

                    copy = new Dictionary<string, string>(tags);
                    copy["statistic"] = "mean";
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, unit, copy, arg.Mean));

                    tags["statistic"] = statistic;
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, unit, tags, arg.Count * arg.Mean));

                    return null;
                },
                (arg) =>
                {
                    if (statistic == "unknown")
                    {
                        statistic = "total";
                    }

                    IDictionary<string, string> copy = new Dictionary<string, string>(tags);
                    copy["statistic"] = "count";
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, "count", copy, arg.Count));

                    copy = new Dictionary<string, string>(tags);
                    copy["statistic"] = "mean";
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, unit, copy, arg.Mean));

                    copy = new Dictionary<string, string>(tags);
                    copy["statistic"] = "max";
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, unit, copy, arg.Max));

                    tags["statistic"] = statistic;
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, unit, tags, arg.Count * arg.Mean));

                    return null;
                },
                (arg) =>
                {
                    if (statistic == "unknown")
                    {
                        statistic = "value";
                    }

                    tags["statistic"] = statistic;
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, unit, tags, arg.LastValue));
                    return null;
                },
                (arg) =>
                {
                    if (statistic == "unknown")
                    {
                        statistic = "value";
                    }

                    tags["statistic"] = statistic;
                    results.Add(new Metric(name, MetricType.GAUGE, timeStamp, unit, tags, arg.LastValue));
                    return null;
                },
                (arg) =>
                {
                    return null;
                });

            return results;
        }

        protected internal string GetStatistic(IAggregation agg, IMeasure measure)
        {
            var result = agg.Match<string>(
                (arg) =>
                {
                    return "total";
                },
                (arg) =>
                {
                    return "count";
                },
                (arg) =>
                {
                    return "total";
                },
                (arg) =>
                {
                    return "total";
                },
                (arg) =>
                {
                    return "value";
                },
                (arg) =>
                {
                    return "unknown";
                });

            if (MeasureUnit.IsTimeUnit(measure.Unit) && result == "total")
            {
                result = "totalTime";
            }

            return result;
        }
    }
}
