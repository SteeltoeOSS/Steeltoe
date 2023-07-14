// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Steeltoe.Connectors.Redis;
using Xunit;

namespace Steeltoe.Connectors.Test.Redis;

public sealed class RedisConnectionStringBuilderTests
{
    [Fact]
    public void Can_merge_properties()
    {
        var builder = new RedisConnectionStringBuilder
        {
            ConnectionString = "redis0:6380,allowAdmin=true"
        };

        builder["port"] = 123;
        builder["username"] = "me@host.com";

        builder.ConnectionString.Should().Be("redis0:123,allowAdmin=true,username=me@host.com");
    }

    [Fact]
    public void Can_extract_properties()
    {
        var builder = new RedisConnectionStringBuilder
        {
            ConnectionString = "redis0:123,allowAdmin=true,username=me@host.com"
        };

        builder["host"].Should().Be("redis0");
        builder["port"].Should().Be("123");
        builder["allowAdmin"].Should().Be("true");
        builder["username"].Should().Be("me@host.com");
    }

    [Fact]
    public void Returns_null_when_getting_known_keyword()
    {
        var builder = new RedisConnectionStringBuilder();

        object? port = builder["port"];

        port.Should().BeNull();
    }

    [Fact]
    public void Throws_when_getting_unknown_keyword()
    {
        var builder = new RedisConnectionStringBuilder();

        Action action = () => _ = builder["bad"];

        action.Should().ThrowExactly<ArgumentException>().WithMessage("Keyword not supported: 'bad'.*");
    }

    [Fact]
    public void Can_get_unknown_keyword_that_was_set_earlier()
    {
        var builder = new RedisConnectionStringBuilder
        {
            ["some"] = "other"
        };

        object? value = builder["some"];
        value.Should().Be("other");
    }
}
