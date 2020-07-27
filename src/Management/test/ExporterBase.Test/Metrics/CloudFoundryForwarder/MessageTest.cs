// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder.Test
{
    public class MessageTest : BaseTest
    {
        [Fact]
        public void Constructor_SetsValues()
        {
            var tags = new Dictionary<string, string>()
            {
                { "foo", "bar" }
            };

            var metric = new Metric("foo.bar", MetricType.GAUGE, 1L, "unit", tags, 100);
            var metrics = new List<Metric>()
            {
                metric
            };

            var instance = new Instance("id", "index", metrics);

            var instances = new List<Instance>() { instance };

            var app = new Application("id", instances);

            var apps = new List<Application>() { app };

            var m = new Message(apps);

            Assert.Same(apps, m.Applications);
        }

        [Fact]
        public void JsonSerialization_ReturnsExpected()
        {
            var tags = new Dictionary<string, string>()
            {
                { "foo", "bar" }
            };

            var metric = new Metric("foo.bar", MetricType.GAUGE, 1L, "unit", tags, 100);
            var metrics = new List<Metric>()
            {
                metric
            };

            var instance = new Instance("id", "index", metrics);

            var instances = new List<Instance>() { instance };

            var app = new Application("id", instances);

            var apps = new List<Application>() { app };

            var m = new Message(apps);

            var result = Serialize(m);
            Assert.Equal("{\"applications\":[{\"id\":\"id\",\"instances\":[{\"id\":\"id\",\"index\":\"index\",\"metrics\":[{\"name\":\"foo.bar\",\"tags\":{\"foo\":\"bar\"},\"timestamp\":1,\"type\":\"gauge\",\"unit\":\"unit\",\"value\":100.0}]}]}]}", result);
        }
    }
}
