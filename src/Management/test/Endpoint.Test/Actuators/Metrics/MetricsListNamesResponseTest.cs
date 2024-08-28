// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Metrics;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Metrics;

public sealed class MetricsListNamesResponseTest : BaseTest
{
    [Fact]
    public void Constructor_SetsValues()
    {
        var names = new HashSet<string>
        {
            "foo.bar",
            "bar.foo"
        };

        var response = new MetricsResponse(names);
        Assert.NotNull(response.Names);
        Assert.Same(names, response.Names);
    }

    [Fact]
    public void JsonSerialization_ReturnsExpected()
    {
        var names = new HashSet<string>
        {
            "foo.bar",
            "bar.foo"
        };

        var response = new MetricsResponse(names);
        string result = Serialize(response);

        result.Should().BeJson("""
            {
              "names": [
                "foo.bar",
                "bar.foo"
              ]
            }
            """);
    }
}
