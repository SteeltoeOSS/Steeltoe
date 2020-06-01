// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder.Test
{
    public class InstanceTest : BaseTest
    {
        [Fact]
        public void Constructor_SetsValues()
        {
            var tags = new Dictionary<string, string>()
            {
                { "foo", "bar" }
            };

            var metric = new Metric("foo.bar", MetricType.GAUGE, 1L, "unit", tags, 100);
            List<Metric> metrics = new List<Metric>()
            {
                metric
            };

            Instance instance = new Instance("id", "index", metrics);

            Assert.Equal("id", instance.Id);
            Assert.Equal("index", instance.Index);
            Assert.Same(metrics, instance.Metrics);
        }

        [Fact]
        public void JsonSerialization_ReturnsExpected()
        {
            var tags = new Dictionary<string, string>()
            {
                { "foo", "bar" }
            };

            var metric = new Metric("foo.bar", MetricType.GAUGE, 1L, "unit", tags, 100);
            List<Metric> metrics = new List<Metric>()
            {
                metric
            };

            Instance instance = new Instance("id", "index", metrics);
            var result = Serialize(instance);
            Assert.Equal("{\"id\":\"id\",\"index\":\"index\",\"metrics\":[{\"name\":\"foo.bar\",\"tags\":{\"foo\":\"bar\"},\"timestamp\":1,\"type\":\"gauge\",\"unit\":\"unit\",\"value\":100.0}]}", result);
        }
    }
}
