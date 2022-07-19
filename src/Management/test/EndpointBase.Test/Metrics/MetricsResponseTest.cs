// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test;

public class MetricsResponseTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var samples = new List<MetricSample>
        {
            new (MetricStatistic.TotalTime, 100.00)
        };

        var tags = new List<MetricTag>
        {
            new ("tag", new HashSet<string> { "tagValue" })
        };

        var resp = new MetricsResponse("foo.bar", samples, tags);
        Assert.Equal("foo.bar", resp.Name);
        Assert.Same(samples, resp.Measurements);
        Assert.Same(tags, resp.AvailableTags);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var samples = new List<MetricSample>
        {
            new (MetricStatistic.TotalTime, 100.1)
        };

        var tags = new List<MetricTag>
        {
            new ("tag", new HashSet<string> { "tagValue" })
        };

        var resp = new MetricsResponse("foo.bar", samples, tags);
        var result = Serialize(resp);
        Assert.Equal("{\"name\":\"foo.bar\",\"measurements\":[{\"statistic\":\"TOTAL_TIME\",\"value\":100.1}],\"availableTags\":[{\"tag\":\"tag\",\"values\":[\"tagValue\"]}]}", result);
    }
}
