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

using OpenCensus.Stats;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test
{
    public class MetricsEndpointTest : BaseTest
    {
        [Fact]
        public void Constructor_ThrowsIfNulls()
        {
            Assert.Throws<ArgumentNullException>(() => new MetricsEndpoint(null, null, null));
            Assert.Throws<ArgumentNullException>(() => new MetricsEndpoint(new MetricsEndpointOptions(), null, null));
        }

        [Fact]
        public void Invoke_WithNullMetricsRequest_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            SetupStats(stats);

            var ep = new MetricsEndpoint(opts, stats);
            var result = ep.Invoke(null);
            Assert.NotNull(result);
            Assert.IsType<MetricsListNamesResponse>(result);
            var resp = result as MetricsListNamesResponse;
            Assert.NotEmpty(resp.Names);
            Assert.Contains("http.server.requests", resp.Names);
            Assert.Contains("jvm.memory.used", resp.Names);
            Assert.Equal(2, resp.Names.Count);

            opts = new MetricsEndpointOptions();
            stats = new OpenCensusStats();

            ep = new MetricsEndpoint(opts, stats);
            result = ep.Invoke(null);
            Assert.NotNull(result);

            Assert.IsType<MetricsListNamesResponse>(result);
            resp = result as MetricsListNamesResponse;
            Assert.Empty(resp.Names);
        }

        [Fact]
        public void Invoke_WithMetricsRequest_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new MetricsEndpoint(opts, stats);

            var testMeasure = MeasureDouble.Create("test.total", "test", MeasureUnit.Bytes);
            SetupTestView(stats, Sum.Create(), testMeasure, "test.test1");

            var context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            long allKeyssum = 0;
            for (var i = 0; i < 10; i++)
            {
                allKeyssum += i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context1);
            }

            var alltags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("a", "v1"),
                new KeyValuePair<string, string>("b", "v1"),
                new KeyValuePair<string, string>("c", "v1")
            };

            var req = new MetricsRequest("test.test1", alltags);
            var resp = ep.Invoke(req) as MetricsResponse;
            Assert.NotNull(resp);

            Assert.Equal("test.test1", resp.Name);

            Assert.NotNull(resp.Measurements);
            Assert.Single(resp.Measurements);
            var sample = resp.Measurements[0];
            Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);
            Assert.Equal(allKeyssum, sample.Value);

            Assert.NotNull(resp.AvailableTags);
            Assert.Equal(3, resp.AvailableTags.Count);

            req = new MetricsRequest("foo.bar", alltags);
            resp = ep.Invoke(req) as MetricsResponse;
            Assert.Null(resp);
        }

        [Fact]
        public void GetTagValuesInColumnOrder_ReturnsExpected()
        {
            var tags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("foo", "bar"),
                new KeyValuePair<string, string>("aaa", "bbb")
            };

            var columns = new List<ITagKey>()
            {
                TagKey.Create("foo"),
                TagKey.Create("aaa")
            };

            var opts = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var ep = new MetricsEndpoint(opts, stats);
            var result = ep.GetTagValuesInColumnOrder(columns, tags);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(TagValue.Create("bar"), result[0]);
            Assert.Equal(TagValue.Create("bbb"), result[1]);

            // Invalid tagkey provided
            columns = new List<ITagKey>()
            {
                TagKey.Create("bar"),
                TagKey.Create("aaa")
            };

            result = ep.GetTagValuesInColumnOrder(columns, tags);
            Assert.Null(result);

            // aaa column not provided
            columns = new List<ITagKey>()
            {
                TagKey.Create("foo"),
                TagKey.Create("aaa"),
                TagKey.Create("bbb"),
            };
            tags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("foo", "bar"),
                new KeyValuePair<string, string>("bbb", "bbb")
            };
            result = ep.GetTagValuesInColumnOrder(columns, tags);
            Assert.Equal(3, result.Count);
            Assert.Equal(TagValue.Create("bar"), result[0]);
            Assert.Null(result[1]);
            Assert.Equal(TagValue.Create("bbb"), result[2]);
        }

        [Fact]
        public void GetStatistic_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var ep = new MetricsEndpoint(opts, stats);

            var m1 = MeasureDouble.Create("test.totalTime", "test", MeasureUnit.Seconds);
            var result = ep.GetStatistic(Sum.Create(), m1);
            Assert.Equal(MetricStatistic.TOTALTIME, result);

            var m2 = MeasureDouble.Create("test.value", "test", MeasureUnit.Seconds);
            result = ep.GetStatistic(LastValue.Create(), m2);
            Assert.Equal(MetricStatistic.VALUE, result);

            var m3 = MeasureDouble.Create("test.count", "test", MeasureUnit.Seconds);
            result = ep.GetStatistic(Count.Create(), m3);
            Assert.Equal(MetricStatistic.COUNT, result);

            var m4 = MeasureDouble.Create("test.sum", "test", MeasureUnit.Bytes);
            result = ep.GetStatistic(Sum.Create(), m4);
            Assert.Equal(MetricStatistic.TOTAL, result);

            var m5 = MeasureDouble.Create("foobar", "test", MeasureUnit.Seconds);
            result = ep.GetStatistic(Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })), m5);
            Assert.Equal(MetricStatistic.TOTALTIME, result);

            var m6 = MeasureDouble.Create("foobar", "test", MeasureUnit.Bytes);
            result = ep.GetStatistic(Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })), m6);
            Assert.Equal(MetricStatistic.TOTAL, result);
        }

        [Fact]
        public void GetMetricSamples_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var ep = new MetricsEndpoint(opts, stats);

            SetupTestView(stats, Sum.Create(), null, "test.test1");
            var viewData = stats.ViewManager.GetView(ViewName.Create("test.test1"));
            IAggregationData aggData = SumDataDouble.Create(100);

            Assert.NotNull(viewData);
            var result = ep.GetMetricSamples(aggData, viewData);
            Assert.NotNull(result);
            Assert.Single(result);
            var sample = result[0];
            Assert.Equal(100, sample.Value);
            Assert.Equal(MetricStatistic.TOTALTIME, sample.Statistic);

            SetupTestView(stats, Sum.Create(), null, "test.test2");
            viewData = stats.ViewManager.GetView(ViewName.Create("test.test2"));
            aggData = SumDataLong.Create(100);

            Assert.NotNull(viewData);
            result = ep.GetMetricSamples(aggData, viewData);
            Assert.NotNull(result);
            Assert.Single(result);
            sample = result[0];
            Assert.Equal(100, sample.Value);
            Assert.Equal(MetricStatistic.TOTALTIME, sample.Statistic);

            SetupTestView(stats, Count.Create(), null, "test.test3");
            viewData = stats.ViewManager.GetView(ViewName.Create("test.test3"));
            aggData = CountData.Create(100);

            Assert.NotNull(viewData);
            result = ep.GetMetricSamples(aggData, viewData);
            Assert.NotNull(result);
            Assert.Single(result);
            sample = result[0];
            Assert.Equal(100, sample.Value);
            Assert.Equal(MetricStatistic.COUNT, sample.Statistic);

            SetupTestView(stats, Mean.Create(), null, "test.test4");
            viewData = stats.ViewManager.GetView(ViewName.Create("test.test4"));
            aggData = MeanData.Create(100, 50, 1, 500);

            Assert.NotNull(viewData);
            result = ep.GetMetricSamples(aggData, viewData);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            sample = result[0];
            Assert.Equal(50, sample.Value);
            Assert.Equal(MetricStatistic.COUNT, sample.Statistic);
            sample = result[1];
            Assert.Equal(100 * 50, sample.Value);
            Assert.Equal(MetricStatistic.TOTALTIME, sample.Statistic);

            SetupTestView(stats, Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 10.0, 20.0 })), null, "test.test5");
            viewData = stats.ViewManager.GetView(ViewName.Create("test.test5"));
            aggData = DistributionData.Create(100, 50, 5, 200, 5, new List<long>() { 10, 20, 20 });

            Assert.NotNull(viewData);
            result = ep.GetMetricSamples(aggData, viewData);
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            sample = result[0];
            Assert.Equal(50, sample.Value);
            Assert.Equal(MetricStatistic.COUNT, sample.Statistic);

            sample = result[1];
            Assert.Equal(200, sample.Value);
            Assert.Equal(MetricStatistic.MAX, sample.Statistic);

            sample = result[2];
            Assert.Equal(100 * 50, sample.Value);
            Assert.Equal(MetricStatistic.TOTALTIME, sample.Statistic);
        }

        [Fact]
        public void GetAvailableTags_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var ep = new MetricsEndpoint(opts, stats);

            SetupTestView(stats, Sum.Create(), null, "test.test1");
            var viewData = stats.ViewManager.GetView(ViewName.Create("test.test1"));

            var dict = new Dictionary<TagValues, IAggregationData>()
            {
                {
                TagValues.Create(new List<ITagValue>()
                {
                    TagValue.Create("v1"), TagValue.Create("v1"), TagValue.Create("v1")
                }),
                SumDataDouble.Create(1)
                },
                {
                TagValues.Create(new List<ITagValue>()
                {
                    TagValue.Create("v2"), TagValue.Create("v2"), TagValue.Create("v2")
                }),
                SumDataDouble.Create(1)
                }
            };

            var result = ep.GetAvailableTags(viewData.View.Columns, dict);

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            var tag = result[0];
            Assert.Equal("a", tag.Tag);
            Assert.Contains("v1", tag.Values);
            Assert.Contains("v2", tag.Values);

            tag = result[1];
            Assert.Equal("b", tag.Tag);
            Assert.Contains("v1", tag.Values);
            Assert.Contains("v2", tag.Values);

            tag = result[2];
            Assert.Equal("c", tag.Tag);
            Assert.Contains("v1", tag.Values);
            Assert.Contains("v2", tag.Values);

            dict = new Dictionary<TagValues, IAggregationData>();
            result = ep.GetAvailableTags(viewData.View.Columns, dict);
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            tag = result[0];
            Assert.Equal("a", tag.Tag);
            Assert.Empty(tag.Values);

            tag = result[1];
            Assert.Equal("b", tag.Tag);
            Assert.Empty(tag.Values);

            tag = result[2];
            Assert.Equal("c", tag.Tag);
            Assert.Empty(tag.Values);
        }

        [Fact]
        public void GetMetricMeasurements_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new MetricsEndpoint(opts, stats);

            var testMeasure = MeasureDouble.Create("test.total", "test", MeasureUnit.Bytes);
            SetupTestView(stats, Sum.Create(), testMeasure, "test.test1");

            var context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            var context2 = tagger
                 .EmptyBuilder
                 .Put(TagKey.Create("a"), TagValue.Create("v1"))
                 .Build();

            var context3 = tagger
                 .EmptyBuilder
                 .Put(TagKey.Create("b"), TagValue.Create("v1"))
                 .Build();

            var context4 = tagger
                 .EmptyBuilder
                 .Put(TagKey.Create("c"), TagValue.Create("v1"))
                 .Build();

            long allKeyssum = 0;
            for (var i = 0; i < 10; i++)
            {
                allKeyssum += i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context1);
            }

            long asum = 0;
            for (var i = 0; i < 10; i++)
            {
                asum += i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context2);
            }

            long bsum = 0;
            for (var i = 0; i < 10; i++)
            {
                bsum += i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context3);
            }

            long csum = 0;
            for (var i = 0; i < 10; i++)
            {
                csum += i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context4);
            }

            var alltags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("a", "v1"),
                new KeyValuePair<string, string>("b", "v1"),
                new KeyValuePair<string, string>("c", "v1")
            };

            var viewData = stats.ViewManager.GetView(ViewName.Create("test.test1"));
            var result = ep.GetMetricMeasurements(viewData, alltags);
            Assert.NotNull(result);
            Assert.Single(result);
            var sample = result[0];
            Assert.Equal(allKeyssum, sample.Value);
            Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);

            var atags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("a", "v1"),
            };

            result = ep.GetMetricMeasurements(viewData, atags);
            Assert.NotNull(result);
            Assert.Single(result);
            sample = result[0];
            Assert.Equal(allKeyssum + asum, sample.Value);
            Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);

            var btags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("b", "v1"),
            };

            result = ep.GetMetricMeasurements(viewData, btags);
            Assert.NotNull(result);
            Assert.Single(result);
            sample = result[0];
            Assert.Equal(allKeyssum + bsum, sample.Value);
            Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);

            var ctags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("c", "v1"),
            };

            result = ep.GetMetricMeasurements(viewData, ctags);
            Assert.NotNull(result);
            Assert.Single(result);
            sample = result[0];
            Assert.Equal(allKeyssum + csum, sample.Value);
            Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);

            var abtags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("a", "v1"),
                new KeyValuePair<string, string>("b", "v1"),
            };

            result = ep.GetMetricMeasurements(viewData, abtags);
            Assert.NotNull(result);
            Assert.Single(result);
            sample = result[0];
            Assert.Equal(allKeyssum, sample.Value);
            Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);

            var actags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("a", "v1"),
                new KeyValuePair<string, string>("c", "v1"),
            };

            result = ep.GetMetricMeasurements(viewData, actags);
            Assert.NotNull(result);
            Assert.Single(result);
            sample = result[0];
            Assert.Equal(allKeyssum, sample.Value);
            Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);

            var bctags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("b", "v1"),
                new KeyValuePair<string, string>("c", "v1"),
            };

            result = ep.GetMetricMeasurements(viewData, bctags);
            Assert.NotNull(result);
            Assert.Single(result);
            sample = result[0];
            Assert.Equal(allKeyssum, sample.Value);
            Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);
        }

        [Fact]
        public void GetMetric_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new MetricsEndpoint(opts, stats);

            var testMeasure = MeasureDouble.Create("test.total", "test", MeasureUnit.Bytes);
            SetupTestView(stats, Sum.Create(), testMeasure, "test.test1");

            var context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            long allKeyssum = 0;
            for (var i = 0; i < 10; i++)
            {
                allKeyssum += i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context1);
            }

            var alltags = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("a", "v1"),
                new KeyValuePair<string, string>("b", "v1"),
                new KeyValuePair<string, string>("c", "v1")
            };

            var req = new MetricsRequest("test.test1", alltags);
            var resp = ep.GetMetric(req);
            Assert.NotNull(resp);

            Assert.Equal("test.test1", resp.Name);

            Assert.NotNull(resp.Measurements);
            Assert.Single(resp.Measurements);
            var sample = resp.Measurements[0];
            Assert.Equal(MetricStatistic.TOTAL, sample.Statistic);
            Assert.Equal(allKeyssum, sample.Value);

            Assert.NotNull(resp.AvailableTags);
            Assert.Equal(3, resp.AvailableTags.Count);
        }

        private void SetupStats(OpenCensusStats stats)
        {
            var exceptionKey = TagKey.Create("exception");
            var methodKey = TagKey.Create("method");
            var uriKey = TagKey.Create("uri");
            var statusKey = TagKey.Create("status");

            var httpServerRquestMeasure = MeasureDouble.Create("server.totalTime", "server request times", MeasureUnit.MilliSeconds);

            var httpServerRequestsViewName = ViewName.Create("http.server.requests");
            var httpServerRequestsView = View.Create(
                                        httpServerRequestsViewName,
                                        "server request times",
                                        httpServerRquestMeasure,
                                        Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 2.0 })),
                                        new List<ITagKey>() { exceptionKey, methodKey, uriKey, statusKey });

            stats.ViewManager.RegisterView(httpServerRequestsView);

            var area = TagKey.Create("area");
            var id = TagKey.Create("id");

            var memoryUsageMeasure = MeasureDouble.Create("memory.value", "memory usage", MeasureUnit.Bytes);

            var memoryUsageName = ViewName.Create("jvm.memory.used");
            var memoryUsageView = View.Create(
                                        memoryUsageName,
                                        "memory usage",
                                        memoryUsageMeasure,
                                        Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1000.0, 10000.0, 100000.0, 1000000.0, 10000000.0, 100000000.0 })),
                                        new List<ITagKey>() { area, id });

            stats.ViewManager.RegisterView(memoryUsageView);
        }

        private void SetupTestView(OpenCensusStats stats, IAggregation agg,  IMeasure measure = null, string viewName = "test.test")
        {
            var aKey = TagKey.Create("a");
            var bKey = TagKey.Create("b");
            var cKey = TagKey.Create("c");

            if (measure == null)
            {
                measure = MeasureDouble.Create(Guid.NewGuid().ToString(), "test", MeasureUnit.MilliSeconds);
            }

            var testViewName = ViewName.Create(viewName);
            var testView = View.Create(
                                        testViewName,
                                        "test",
                                        measure,
                                        agg,
                                        new List<ITagKey>() { aKey, bKey, cKey });

            stats.ViewManager.RegisterView(testView);
        }
    }
}
