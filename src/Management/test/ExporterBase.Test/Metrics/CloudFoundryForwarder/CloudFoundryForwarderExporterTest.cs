// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Stats;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using Xunit;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder.Test
{
    public class CloudFoundryForwarderExporterTest : BaseTest
    {
        [Fact]
        public void CreateMetricsFromViewData_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions()
            {
                MicrometerMetricWriter = true
            };

            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new CloudFoundryForwarderExporter(opts, stats);

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

            var viewData = stats.ViewManager.GetView(ViewName.Create("test.test1"));
            var result = ep.CreateMetricsFromViewData(viewData, 1L);

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
        public void GetMetricsForExportedViews_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions()
            {
                MicrometerMetricWriter = true
            };

            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new CloudFoundryForwarderExporter(opts, stats);

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

            var result = ep.GetMetricsForExportedViews(stats.ViewManager.AllExportedViews, 1L);

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
        public void GetMessage_ReturnsExpected()
        {
            var opts = new CloudFoundryForwarderOptions()
            {
                InstanceId = "InstanceId",
                InstanceIndex = "InstanceIndex",
                ApplicationId = "ApplicationId",
                MicrometerMetricWriter = true
            };
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new CloudFoundryForwarderExporter(opts, stats);

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

            var message = ep.GetMessage(stats.ViewManager.AllExportedViews, 1L);

            Assert.NotNull(message);
            var result = Serialize(message);
            Assert.Equal("{\"applications\":[{\"id\":\"ApplicationId\",\"instances\":[{\"id\":\"InstanceId\",\"index\":\"InstanceIndex\",\"metrics\":[{\"name\":\"test.test1\",\"tags\":{\"a\":\"v1\",\"b\":\"v1\",\"c\":\"v1\",\"statistic\":\"total\"},\"timestamp\":1,\"type\":\"gauge\",\"unit\":\"bytes\",\"value\":45.0}]}]}]}", result);
        }
    }
}
