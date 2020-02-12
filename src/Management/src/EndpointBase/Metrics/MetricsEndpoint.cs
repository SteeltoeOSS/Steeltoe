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
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricsEndpoint : AbstractEndpoint<IMetricsResponse, MetricsRequest>
    {
        private readonly ILogger<MetricsEndpoint> _logger;
        private readonly IStats _stats;

        public MetricsEndpoint(IMetricsOptions options, IStats stats,  ILogger<MetricsEndpoint> logger = null)
            : base(options)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _logger = logger;
        }

        public new IMetricsOptions Options
        {
            get
            {
                return options as IMetricsOptions;
            }
        }

        public override IMetricsResponse Invoke(MetricsRequest request)
        {
            //var names = GetMetricNames();

            //if (request == null)
            //{
            //    return new MetricsListNamesResponse(names);
            //}
            //else
            //{
            //    if (names.Contains(request.MetricName))
            //    {
            //        return GetMetric(request);
            //    }
            //}

            return null;
        }

        protected internal MetricsResponse GetMetric(MetricsRequest request)
        {
        //    var viewData = null; // _stats.ViewManager.GetView(ViewName.Create(request.MetricName)); How to??
        //    if (viewData == null)
        //    {
        //        return null;
        //    }

        //    List<MetricSample> measurements = GetMetricMeasurements(viewData, request.Tags);
        //    List<MetricTag> availTags = GetAvailableTags(viewData);

        //    return new MetricsResponse(request.MetricName, measurements, availTags);
        return new MetricsResponse(null, null, null);
        }

    //    protected internal List<MetricTag> GetAvailableTags(IViewData viewData)
    //    {
    //        return GetAvailableTags(viewData.View.Columns, viewData.AggregationMap);
    //    }

    //    protected internal List<MetricTag> GetAvailableTags(IList<ITagKey> columns, IDictionary<TagValues, IAggregationData> aggMap)
    //    {
    //        List<MetricTag> results = new List<MetricTag>();

    //        for (int i = 0; i < columns.Count; i++)
    //        {
    //            string tag = columns[i].Name;
    //            HashSet<string> set = new HashSet<string>();

    //            foreach (var agg in aggMap)
    //            {
    //                var tagValue = agg.Key.Values[i];
    //                if (tagValue != null)
    //                {
    //                    set.Add(tagValue.AsString);
    //                }
    //            }

    //            results.Add(new MetricTag(tag, set));
    //        }

    //        return results;
    //    }

    //    protected internal List<MetricSample> GetMetricMeasurements(IViewData viewData, List<KeyValuePair<string, string>> tags)
    //    {
    //        var tagValues = GetTagValuesInColumnOrder(viewData.View.Columns, tags);
    //        if (tagValues == null)
    //        {
    //            // One or more of tag keys are invalid
    //            return new List<MetricSample>();
    //        }

    //        IAggregationData agg = MetricsHelpers.SumWithTags(viewData, tagValues);
    //        return GetMetricSamples(agg, viewData);
    //    }

    //    protected internal List<MetricSample> GetMetricSamples(IAggregationData agg, IViewData viewData)
    //    {
    //        List<MetricSample> results = new List<MetricSample>();
    //        MetricStatistic statistic = GetStatistic(viewData.View.Aggregation, viewData.View.Measure);

    //        agg.Match<object>(
    //            (arg) =>
    //            {
    //                if (statistic == MetricStatistic.UNKNOWN)
    //                {
    //                    statistic = MetricStatistic.TOTAL;
    //                }

    //                results.Add(new MetricSample(statistic, arg.Sum));
    //                return null;
    //            },
    //            (arg) =>
    //            {
    //                if (statistic == MetricStatistic.UNKNOWN)
    //                {
    //                    statistic = MetricStatistic.TOTAL;
    //                }

    //                results.Add(new MetricSample(statistic, arg.Sum));
    //                return null;
    //            },
    //            (arg) =>
    //            {
    //                if (statistic == MetricStatistic.UNKNOWN)
    //                {
    //                    statistic = MetricStatistic.COUNT;
    //                }

    //                results.Add(new MetricSample(statistic, arg.Count));
    //                return null;
    //            },
    //            (arg) =>
    //            {
    //                results.Add(new MetricSample(MetricStatistic.COUNT, arg.Count));
    //                if (statistic == MetricStatistic.UNKNOWN)
    //                {
    //                    statistic = MetricStatistic.TOTAL;
    //                }

    //                results.Add(new MetricSample(statistic, arg.Mean * arg.Count));
    //                return null;
    //            },
    //            (arg) =>
    //            {
    //                results.Add(new MetricSample(MetricStatistic.COUNT, arg.Count));
    //                results.Add(new MetricSample(MetricStatistic.MAX, arg.Max));
    //                if (statistic == MetricStatistic.UNKNOWN)
    //                {
    //                    statistic = MetricStatistic.TOTAL;
    //                }

    //                results.Add(new MetricSample(statistic, arg.Mean * arg.Count));
    //                return null;
    //            },
    //            (arg) =>
    //            {
    //                if (statistic == MetricStatistic.UNKNOWN)
    //                {
    //                    statistic = MetricStatistic.VALUE;
    //                }

    //                results.Add(new MetricSample(statistic, arg.LastValue));
    //                return null;
    //            },
    //            (arg) =>
    //            {
    //                if (statistic == MetricStatistic.UNKNOWN)
    //                {
    //                    statistic = MetricStatistic.VALUE;
    //                }

    //                results.Add(new MetricSample(statistic, arg.LastValue));
    //                return null;
    //            },
    //            (arg) =>
    //            {
    //                return null;
    //            });

    //        return results;
    //    }

    //    protected internal MetricStatistic GetStatistic(IAggregation agg, IMeasure measure)
    //    {
    //        var result = agg.Match<MetricStatistic>(
    //            (arg) =>
    //            {
    //                return MetricStatistic.TOTAL;
    //            },
    //            (arg) =>
    //            {
    //                return MetricStatistic.COUNT;
    //            },
    //            (arg) =>
    //            {
    //                return MetricStatistic.TOTAL;
    //            },
    //            (arg) =>
    //            {
    //                return MetricStatistic.TOTAL;
    //            },
    //            (arg) =>
    //            {
    //                return MetricStatistic.VALUE;
    //            },
    //            (arg) =>
    //            {
    //                return MetricStatistic.UNKNOWN;
    //            });

    //        if (MeasureUnit.IsTimeUnit(measure.Unit) && result == MetricStatistic.TOTAL)
    //        {
    //            result = MetricStatistic.TOTALTIME;
    //        }

    //        return result;
    //    }

    //    protected internal List<ITagValue> GetTagValuesInColumnOrder(IList<ITagKey> columns, List<KeyValuePair<string, string>> tags)
    //    {
    //        ITagValue[] tagValues = new ITagValue[columns.Count];
    //        foreach (var kvp in tags)
    //        {
    //            var key = TagKey.Create(kvp.Key);
    //            var value = TagValue.Create(kvp.Value);
    //            int indx = columns.IndexOf(key);
    //            if (indx < 0)
    //            {
    //                return null;
    //            }

    //            tagValues[indx] = value;
    //        }

    //        return new List<ITagValue>(tagValues);
    //    }

    //    protected internal ISet<string> GetMetricNames()
    //    {
    //        var allViews = _stats.ViewManager.AllExportedViews;
    //        HashSet<string> names = new HashSet<string>();
    //        foreach (var view in allViews)
    //        {
    //            names.Add(view.Name.AsString);
    //        }

    //        return names;
    //    }
    }
}
