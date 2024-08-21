// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Metrics;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Metrics;

public sealed class MetricTagTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var tags = new HashSet<string>
        {
            "tagValue"
        };

        var metricTag = new MetricTag("tagname", tags);
        Assert.Equal("tagname", metricTag.Tag);
        Assert.Same(tags, metricTag.Values);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var tags = new HashSet<string>
        {
            "tagValue"
        };

        var metricTag = new MetricTag("tagname", tags);
        string result = Serialize(metricTag);
        Assert.Equal("{\"tag\":\"tagname\",\"values\":[\"tagValue\"]}", result);
    }
}
