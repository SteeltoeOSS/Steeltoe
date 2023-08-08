// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public sealed class MetricsResponseTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var samples = new List<MetricSample>
        {
            new(MetricStatistic.TotalTime, 100.00, null)
        };

        var tags = new List<MetricTag>
        {
            new("tag", new HashSet<string>
            {
                "tagValue"
            })
        };

        var response = new MetricsResponse("foo.bar", samples, tags);
        Assert.Equal("foo.bar", response.Name);
        Assert.Same(samples, response.Measurements);
        Assert.Same(tags, response.AvailableTags);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var samples = new List<MetricSample>
        {
            new(MetricStatistic.TotalTime, 100.1, null)
        };

        var tags = new List<MetricTag>
        {
            new("tag", new HashSet<string>
            {
                "tagValue"
            })
        };

        var response = new MetricsResponse("foo.bar", samples, tags);
        string result = Serialize(response);

        Assert.Equal(
            "{\"name\":\"foo.bar\",\"measurements\":[{\"statistic\":\"TOTAL_TIME\",\"value\":100.1}],\"availableTags\":[{\"tag\":\"tag\",\"values\":[\"tagValue\"]}]}",
            result);
    }
}
