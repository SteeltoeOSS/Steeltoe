// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public class MetricsListNamesResponseTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var names = new HashSet<string>
        {
            "foo.bar",
            "bar.foo"
        };

        var resp = new MetricsResponse(names);
        Assert.NotNull(resp.Names);
        Assert.Same(names, resp.Names);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var names = new HashSet<string>
        {
            "foo.bar",
            "bar.foo"
        };

        var resp = new MetricsResponse(names);
        string result = Serialize(resp);
        Assert.Equal("{\"names\":[\"foo.bar\",\"bar.foo\"]}", result);
    }
}
