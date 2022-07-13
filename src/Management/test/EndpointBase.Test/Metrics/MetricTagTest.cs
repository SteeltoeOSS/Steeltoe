// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test;

public class MetricTagTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var tags = new HashSet<string> { "tagValue" };
        var metricTag = new MetricTag("tagname", tags);
        Assert.Equal("tagname", metricTag.Tag);
        Assert.Same(tags, metricTag.Values);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var tags = new HashSet<string> { "tagValue" };
        var metricTag = new MetricTag("tagname", tags);
        var result = Serialize(metricTag);
        Assert.Equal("{\"tag\":\"tagname\",\"values\":[\"tagValue\"]}", result);
    }
}
