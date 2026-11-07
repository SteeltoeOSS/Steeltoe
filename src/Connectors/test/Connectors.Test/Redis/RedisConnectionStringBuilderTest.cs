// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connectors.Redis;

namespace Steeltoe.Connectors.Test.Redis;

public sealed class RedisConnectionStringBuilderTest
{
    [Fact]
    public void Can_merge_properties()
    {
        var builder = new RedisConnectionStringBuilder
        {
            ConnectionString = "redis0:6380,allowAdmin=true",
            ["port"] = 123,
            ["username"] = "me@host.com"
        };

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
    public void Returns_null_when_getting_unknown_keyword()
    {
        var builder = new RedisConnectionStringBuilder();

        object? some = builder["some"];

        some.Should().BeNull();
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

    [Theory]
    [InlineData("redis0:6380,redis1:6380")]
    [InlineData("redis0,allowAdmin=true,redis1")]
    public void Throws_on_multiple_servers(string connectionString)
    {
        var builder = new RedisConnectionStringBuilder();

        Action action = () => builder.ConnectionString = connectionString;

        action.Should().ThrowExactly<NotImplementedException>();
    }

    [Fact]
    public void Can_replace_connection_string_having_server()
    {
        var builder = new RedisConnectionStringBuilder
        {
            ConnectionString = "redis0:6380"
        };

        builder.ConnectionString = "redis1:6381";

        builder.ConnectionString.Should().Be("redis1:6381");
    }
}
