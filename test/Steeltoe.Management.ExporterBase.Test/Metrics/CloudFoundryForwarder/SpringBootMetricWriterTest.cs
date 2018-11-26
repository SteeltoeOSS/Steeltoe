// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder.Test
{
    public class SpringBootMetricWriterTest : BaseTest
    {
        [Fact]
        public void GetTagKeysAndValues_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var ep = new SpringBootMetricWriter(opts, stats);

            IList<ITagKey> keys = new List<ITagKey>()
            {
                TagKey.Create("status"), TagKey.Create("exception"), TagKey.Create("method"), TagKey.Create("uri")
            };
            IList<ITagValue> values = new List<ITagValue>()
            {
                TagValue.Create("v1"), TagValue.Create("v2"), TagValue.Create("v3"), TagValue.Create("v4")
            };

            var result = ep.GetTagKeysAndValues(keys, values);
            Assert.Equal("v1", result["status"]);
            Assert.Equal("v2", result["exception"]);
            Assert.Equal("v3", result["method"]);
            Assert.Equal("v4", result["uri"]);

            // Verify sorted
            var sortedKeys = result.Keys.ToList();
            Assert.Equal("exception", sortedKeys[0]);
            Assert.Equal("method", sortedKeys[1]);
            Assert.Equal("status", sortedKeys[2]);
            Assert.Equal("uri", sortedKeys[3]);

            values = new List<ITagValue>()
            {
                TagValue.Create("v1"), null, null, null
            };

            result = ep.GetTagKeysAndValues(keys, values);
            Assert.Equal("v1", result["status"]);
            Assert.Single(result);

            values = new List<ITagValue>()
            {
                null, TagValue.Create("v2"), null, null
            };

            result = ep.GetTagKeysAndValues(keys, values);
            Assert.Equal("v2", result["exception"]);
            Assert.Single(result);

            values = new List<ITagValue>()
            {
                TagValue.Create("v1"),
            };

            result = ep.GetTagKeysAndValues(keys, values);
            Assert.Empty(result);
        }

        [Fact]
        public void GetTagValue_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var ep = new SpringBootMetricWriter(opts, stats);

            Assert.Equal("root", ep.GetTagValue("/"));
            Assert.Equal(".foo.bar", ep.GetTagValue("/foo/bar"));
            Assert.Equal("foo.bar", ep.GetTagValue("foo/bar"));
            Assert.Equal("bar", ep.GetTagValue("bar"));
        }

        [Fact]
        public void ShouldSkipTag_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var ep = new SpringBootMetricWriter(opts, stats);

            Assert.True(ep.ShouldSkipTag("exception"));
            Assert.True(ep.ShouldSkipTag("clientName"));
            Assert.False(ep.ShouldSkipTag("foobar"));
        }

        [Fact]
        public void GetMetricName_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var ep = new SpringBootMetricWriter(opts, stats);

            IList<ITagKey> keys = new List<ITagKey>()
            {
                TagKey.Create("status"), TagKey.Create("exception"), TagKey.Create("method"), TagKey.Create("uri")
            };
            IList<ITagValue> values = new List<ITagValue>()
            {
                TagValue.Create("200"), TagValue.Create("None"), TagValue.Create("GET"), TagValue.Create("/foo/bar")
            };

            var tagDict = ep.GetTagKeysAndValues(keys, values);

            Assert.Equal("http.server.requests.mean.GET.200.foo.bar", ep.GetMetricName("http.server.requests", "mean", tagDict));
            Assert.Equal("http.server.requests.mean", ep.GetMetricName("http.server.requests", "mean", new Dictionary<string, string>()));

            keys = new List<ITagKey>()
            {
                TagKey.Create("foo"), TagKey.Create("bar")
            };
            values = new List<ITagValue>()
            {
                TagValue.Create("foo"), TagValue.Create("bar")
            };
            tagDict = ep.GetTagKeysAndValues(keys, values);
            Assert.Equal("http.server.requests.bar.foo", ep.GetMetricName("http.server.requests", null, tagDict));
        }

        [Fact]
        public void CreateMetrics_SumDoubleAgg_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions();
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new SpringBootMetricWriter(opts, stats);

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
            Assert.Equal("test.test1.v1.v1.v1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal(allKeyssum, metric.Value);
            var tags = metric.Tags;
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
            var ep = new SpringBootMetricWriter(opts, stats);

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
            Assert.Equal("test.test1.v1.v1.v1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal(allKeyssum, metric.Value);
            var tags = metric.Tags;
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
            var ep = new SpringBootMetricWriter(opts, stats);

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
            Assert.Equal("test.test1.value.v1.v1.v1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal(9, metric.Value);
            var tags = metric.Tags;
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
            var ep = new SpringBootMetricWriter(opts, stats);

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
            Assert.Equal(1, result.Count);
            var metric = result[0];
            Assert.Equal("test.test1.v1.v1.v1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal((double)allKeyssum / 10.0, metric.Value);
            var tags = metric.Tags;
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
            var ep = new SpringBootMetricWriter(opts, stats);

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
            Assert.Equal("test.test1.v1.v1.v1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("seconds", metric.Unit);
            Assert.Equal((double)allKeyssum / 10.0, metric.Value);
            var tags = metric.Tags;
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);

            metric = result[1];
            Assert.Equal("test.test1.max.v1.v1.v1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("seconds", metric.Unit);
            Assert.Equal(9, metric.Value);
            tags = metric.Tags;
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);

            metric = result[2];
            Assert.Equal("test.test1.min.v1.v1.v1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("seconds", metric.Unit);
            Assert.Equal(0, metric.Value);
            tags = metric.Tags;
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);

            metric = result[3];
            Assert.Equal("test.test1.stddev.v1.v1.v1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("seconds", metric.Unit);
            Assert.InRange(metric.Value, 2.0d, 3.0d);
            tags = metric.Tags;
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
            var ep = new SpringBootMetricWriter(opts, stats);

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
            Assert.Equal("test.test1.v1.v1.v1", metric.Name);
            Assert.Equal(1L, metric.Timestamp);
            Assert.Equal("gauge", metric.Type);
            Assert.Equal("bytes", metric.Unit);
            Assert.Equal(10, metric.Value);
            var tags = metric.Tags;
            Assert.Equal("v1", tags["a"]);
            Assert.Equal("v1", tags["b"]);
            Assert.Equal("v1", tags["c"]);
        }
    }
}
