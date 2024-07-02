// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Metrics;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public sealed class MetricsRequestTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var tags = new List<KeyValuePair<string, string>>();
        var request = new MetricsRequest("foo.bar", tags);
        Assert.Equal("foo.bar", request.MetricName);
        Assert.Same(tags, request.Tags);
    }
}
