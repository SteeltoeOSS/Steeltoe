// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info;

public sealed class InfoBuilderTest
{
    [Fact]
    public void ReturnsEmptyDictionary()
    {
        var builder = new InfoBuilder();
        IDictionary<string, object?> built = builder.Build();

        built.Should().BeEmpty();
    }

    [Fact]
    public void WithInfoSingleValueAddsValue()
    {
        var builder = new InfoBuilder();
        builder.WithInfo("foo", "bar");
        IDictionary<string, object?> built = builder.Build();

        built.Should().ContainSingle();
        built.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
    }

    [Fact]
    public void WithInfoDictionaryAddsValues()
    {
        var builder = new InfoBuilder();

        var items = new Dictionary<string, object?>
        {
            ["foo"] = "bar",
            ["bar"] = 100,
            ["baz"] = null
        };

        builder.WithInfo(items);
        IDictionary<string, object?> built = builder.Build();

        built.Should().HaveCount(3);
        built.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        built.Should().ContainKey("bar").WhoseValue.Should().Be(100);
        built.Should().ContainKey("baz").WhoseValue.Should().BeNull();
    }
}
