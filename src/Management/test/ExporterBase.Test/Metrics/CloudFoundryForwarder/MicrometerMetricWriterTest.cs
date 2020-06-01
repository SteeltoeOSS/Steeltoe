// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Stats;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder.Test
{
    public class MicrometerMetricWriterTest : BaseTest
    {
        [Fact]
        public void GetStatistic_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var ep = new MicrometerMetricWriter(opts, stats);

            var m1 = MeasureDouble.Create("test.totalTime", "test", MeasureUnit.Seconds);
            var result = ep.GetStatistic(Sum.Create(), m1);
            Assert.Equal("totalTime", result);

            var m2 = MeasureDouble.Create("test.value", "test", MeasureUnit.Seconds);
            result = ep.GetStatistic(LastValue.Create(), m2);
            Assert.Equal("value", result);

            var m3 = MeasureDouble.Create("test.count", "test", MeasureUnit.Seconds);
            result = ep.GetStatistic(Count.Create(), m3);
            Assert.Equal("count", result);

            var m4 = MeasureDouble.Create("test.sum", "test", MeasureUnit.Bytes);
            result = ep.GetStatistic(Sum.Create(), m4);
            Assert.Equal("total", result);

            var m5 = MeasureDouble.Create("foobar", "test", MeasureUnit.Seconds);
            result = ep.GetStatistic(Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })), m5);
            Assert.Equal("totalTime", result);

            var m6 = MeasureDouble.Create("foobar", "test", MeasureUnit.Bytes);
            result = ep.GetStatistic(Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })), m6);
            Assert.Equal("total", result);
        }

        [Fact]
        public void GetTagKeysAndValues_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var ep = new MicrometerMetricWriter(opts, stats);

            IList<ITagKey> keys = new List<ITagKey>()
            {
                TagKey.Create("key1"), TagKey.Create("key2")
            };
            IList<ITagValue> values = new List<ITagValue>()
            {
                TagValue.Create("v1"), TagValue.Create("v2")
            };

            var result = ep.GetTagKeysAndValues(keys, values);
            Assert.Equal("v1", result["key1"]);
            Assert.Equal("v2", result["key2"]);

            values = new List<ITagValue>()
            {
                TagValue.Create("v1"), null
            };

            result = ep.GetTagKeysAndValues(keys, values);
            Assert.Equal("v1", result["key1"]);
            Assert.Single(result);

            values = new List<ITagValue>()
            {
                null, TagValue.Create("v2"),
            };

            result = ep.GetTagKeysAndValues(keys, values);
            Assert.Equal("v2", result["key2"]);
            Assert.Single(result);

            values = new List<ITagValue>()
            {
                TagValue.Create("v1"),
            };

            result = ep.GetTagKeysAndValues(keys, values);
            Assert.Empty(result);
        }

        [Fact]
        public void CreateMetrics_SumDoubleAgg_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new MicrometerMetricWriter(opts, stats);

            IMeasureDouble testMeasure = MeasureDouble.Create("test.total", "test", MeasureUnit.Bytes);
            SetupTestView(stats, Sum.Create(), testMeasure, "test.test1");

            ITagContext context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            long allKeyssum = 0;
            for (int i = 0; i < 10; i++)
            {
                allKeyssum = allKeyssum + i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context1);
            }

            var viewData = stats.ViewManager.GetView(ViewName.Create("test.test1"));
            Assert.NotNull(viewData);
            var aggMap = viewData.AggregationMap;
            Assert.Single(aggMap);

            var tagValues = aggMap.Keys.Single();
            var data = aggMap.Values.Single();
            Assert.NotNull(tagValues);
            Assert.NotNull(data);

            var result = ep.CreateMetrics(viewData, data, tagValues, 1L);
            Assert.NotNull(result);
            Assert.Single(result);
            var metric = result[0];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal(allKeyssum, metric.Value);
            var tags = metric.Tags;
            Assert.Equal("total", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);
        }

        [Fact]
        public void CreateMetrics_SumLongAgg_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new MicrometerMetricWriter(opts, stats);

            IMeasureDouble testMeasure = MeasureDouble.Create("test.total", "test", MeasureUnit.Bytes);
            SetupTestView(stats, Sum.Create(), testMeasure, "test.test1");

            ITagContext context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            long allKeyssum = 0;
            for (int i = 0; i < 10; i++)
            {
                allKeyssum = allKeyssum + i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context1);
            }

            var viewData = stats.ViewManager.GetView(ViewName.Create("test.test1"));
            Assert.NotNull(viewData);
            var aggMap = viewData.AggregationMap;
            Assert.Single(aggMap);

            var tagValues = aggMap.Keys.Single();
            var data = aggMap.Values.Single();
            Assert.NotNull(tagValues);
            Assert.NotNull(data);

            var result = ep.CreateMetrics(viewData, data, tagValues, 1L);
            Assert.NotNull(result);
            Assert.Single(result);
            var metric = result[0];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal(allKeyssum, metric.Value);
            var tags = metric.Tags;
            Assert.Equal("total", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);
        }

        [Fact]
        public void CreateMetrics_LastValueAgg_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new MicrometerMetricWriter(opts, stats);

            IMeasureDouble testMeasure = MeasureDouble.Create("test.total", "test", MeasureUnit.Bytes);
            SetupTestView(stats, LastValue.Create(), testMeasure, "test.test1");

            ITagContext context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            long allKeyssum = 0;
            for (int i = 0; i < 10; i++)
            {
                allKeyssum = allKeyssum + i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context1);
            }

            var viewData = stats.ViewManager.GetView(ViewName.Create("test.test1"));
            Assert.NotNull(viewData);
            var aggMap = viewData.AggregationMap;
            Assert.Single(aggMap);

            var tagValues = aggMap.Keys.Single();
            var data = aggMap.Values.Single();
            Assert.NotNull(tagValues);
            Assert.NotNull(data);

            var result = ep.CreateMetrics(viewData, data, tagValues, 1L);
            Assert.NotNull(result);
            Assert.Single(result);
            var metric = result[0];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal(9, metric.Value);
            var tags = metric.Tags;
            Assert.Equal("value", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);
        }

        [Fact]
        public void CreateMetrics_MeanAgg_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new MicrometerMetricWriter(opts, stats);

            IMeasureDouble testMeasure = MeasureDouble.Create("test.total", "test", MeasureUnit.Bytes);
            SetupTestView(stats, Mean.Create(), testMeasure, "test.test1");

            ITagContext context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            long allKeyssum = 0;
            for (int i = 0; i < 10; i++)
            {
                allKeyssum = allKeyssum + i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context1);
            }

            var viewData = stats.ViewManager.GetView(ViewName.Create("test.test1"));
            Assert.NotNull(viewData);
            var aggMap = viewData.AggregationMap;
            Assert.Single(aggMap);

            var tagValues = aggMap.Keys.Single();
            var data = aggMap.Values.Single();
            Assert.NotNull(tagValues);
            Assert.NotNull(data);

            var result = ep.CreateMetrics(viewData, data, tagValues, 1L);
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            var metric = result[0];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("count", metric.Unit);
            Assert.Equal(10, metric.Value);
            var tags = metric.Tags;
            Assert.Equal("count", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);

            metric = result[1];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal((double)allKeyssum / 10.0, metric.Value);
            tags = metric.Tags;
            Assert.Equal("mean", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);

            metric = result[2];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal(allKeyssum, metric.Value);
            tags = metric.Tags;
            Assert.Equal("total", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);
        }

        [Fact]
        public void CreateMetrics_DistributionAgg_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new MicrometerMetricWriter(opts, stats);

            IMeasureDouble testMeasure = MeasureDouble.Create("test.total", "test", MeasureUnit.Seconds);
            SetupTestView(stats, Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })), testMeasure, "test.test1");

            ITagContext context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            long allKeyssum = 0;
            for (int i = 0; i < 10; i++)
            {
                allKeyssum = allKeyssum + i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context1);
            }

            var viewData = stats.ViewManager.GetView(ViewName.Create("test.test1"));
            Assert.NotNull(viewData);
            var aggMap = viewData.AggregationMap;
            Assert.Single(aggMap);

            var tagValues = aggMap.Keys.Single();
            var data = aggMap.Values.Single();
            Assert.NotNull(tagValues);
            Assert.NotNull(data);

            var result = ep.CreateMetrics(viewData, data, tagValues, 1L);
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);

            var metric = result[0];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("count", metric.Unit);
            Assert.Equal(10, metric.Value);
            var tags = metric.Tags;
            Assert.Equal("count", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);

            metric = result[1];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("seconds", metric.Unit);
            Assert.Equal((double)allKeyssum / 10.0, metric.Value);
            tags = metric.Tags;
            Assert.Equal("mean", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);

            metric = result[2];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("seconds", metric.Unit);
            Assert.Equal(9, metric.Value);
            tags = metric.Tags;
            Assert.Equal("max", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);

            metric = result[3];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("seconds", metric.Unit);
            Assert.Equal(allKeyssum, metric.Value);
            tags = metric.Tags;
            Assert.Equal("totalTime", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);
        }

        [Fact]
        public void CreateMetrics_CountAgg_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new MicrometerMetricWriter(opts, stats);

            IMeasureDouble testMeasure = MeasureDouble.Create("test.total", "test", MeasureUnit.Bytes);
            SetupTestView(stats, Count.Create(), testMeasure, "test.test1");

            ITagContext context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            long allKeyssum = 0;
            for (int i = 0; i < 10; i++)
            {
                allKeyssum = allKeyssum + i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context1);
            }

            var viewData = stats.ViewManager.GetView(ViewName.Create("test.test1"));
            Assert.NotNull(viewData);
            var aggMap = viewData.AggregationMap;
            Assert.Single(aggMap);

            var tagValues = aggMap.Keys.Single();
            var data = aggMap.Values.Single();
            Assert.NotNull(tagValues);
            Assert.NotNull(data);

            var result = ep.CreateMetrics(viewData, data, tagValues, 1L);
            Assert.NotNull(result);
            Assert.Single(result);
            var metric = result[0];
            Assert.Equal("test.test1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal(10, metric.Value);
            var tags = metric.Tags;
            Assert.Equal("count", tags["statistic"]);
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);
        }
    }
}
