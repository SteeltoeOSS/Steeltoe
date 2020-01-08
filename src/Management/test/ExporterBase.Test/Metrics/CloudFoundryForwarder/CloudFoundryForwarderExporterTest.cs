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
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration;
using Steeltoe.Management.Census.Stats;
using System.Collections.Generic;
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
                allKeyssum = allKeyssum + i;
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
                allKeyssum = allKeyssum + i;
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
            var configsource = new Dictionary<string, string>
            {
                { "application:InstanceId", "InstanceId" },
                { "application:InstanceIndex", "1" },
                { "application:ApplicationId", "ApplicationId" },
                { "management:metrics:exporter:cloudfoundry:MicrometerMetricWriter", "true" }
            };
            var config = TestHelpers.GetConfigurationFromDictionary(configsource);
            var opts = new CloudFoundryForwarderOptions(new ApplicationInstanceInfo(config), new ServicesOptions(config), config);
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
                allKeyssum = allKeyssum + i;
                stats.StatsRecorder.NewMeasureMap().Put(testMeasure, i).Record(context1);
            }

            var message = ep.GetMessage(stats.ViewManager.AllExportedViews, 1L);

            Assert.NotNull(message);
            var result = Serialize(message);
            Assert.Equal("{\"applications\":[{\"id\":\"ApplicationId\",\"instances\":[{\"id\":\"InstanceId\",\"index\":\"1\",\"metrics\":[{\"name\":\"test.test1\",\"tags\":{\"a\":\"v1\",\"b\":\"v1\",\"c\":\"v1\",\"statistic\":\"total\"},\"timestamp\":1,\"type\":\"gauge\",\"unit\":\"bytes\",\"value\":45.0}]}]}]}", result);
        }
    }
}
