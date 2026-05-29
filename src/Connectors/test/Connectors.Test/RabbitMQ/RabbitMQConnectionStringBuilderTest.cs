// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connectors.RabbitMQ;

namespace Steeltoe.Connectors.Test.RabbitMQ;

public sealed class RabbitMQConnectionStringBuilderTest
{
    [Fact]
    public void Merges_properties_with_special_characters()
    {
        var builder = new RabbitMQConnectionStringBuilder
        {
            ConnectionString = "amqps://localhost:999/virtual-host-1",
            ["useTls"] = false,
            ["port"] = 123,
            ["username"] = "me@host.com",
            ["password"] = "some \"secret\" value",
            ["virtualHost"] = "my virtual= host"
        };

        builder.ConnectionString.Should().Be("amqp://me%40host.com:some%20%22secret%22%20value@localhost:123/my%20virtual%3D%20host");
        builder["URL"].Should().Be(builder.ConnectionString);
    }

    [Fact]
    public void Decodes_properties_with_special_characters()
    {
        var builder = new RabbitMQConnectionStringBuilder
        {
            ConnectionString = "amqps://me%40host.com:some%20%22secret%22%20value@localhost:123/my%20virtual%3d%20host"
        };

        builder["UseTls"].Should().Be("True");
        builder["host"].Should().Be("localhost");
        builder["port"].Should().Be("123");
        builder["username"].Should().Be("me@host.com");
        builder["password"].Should().Be("some \"secret\" value");
        builder["virtualHost"].Should().Be("my virtual= host");
    }

    [Fact]
    public void Preserves_query_string_parameters_when_setting_URL()
    {
        var builder = new RabbitMQConnectionStringBuilder
        {
            ConnectionString = "amqps://localhost:999/virtual-host-1?first=one&second=two"
        };

        const string url = "amqps://localhost:999/virtual-host-1?first=one&second=number2";
        builder["url"] = url;

        builder.ConnectionString.Should().Be("amqps://localhost:999/virtual-host-1?first=one&second=number2");
        builder["url"].Should().Be(builder.ConnectionString);
    }

    [Fact]
    public void Returns_null_when_getting_known_keyword()
    {
        var builder = new RabbitMQConnectionStringBuilder();

        object? port = builder["port"];

        port.Should().BeNull();
    }

    [Fact]
    public void Returns_null_when_getting_unknown_keyword()
    {
        var builder = new RabbitMQConnectionStringBuilder();

        object? some = builder["some"];

        some.Should().BeNull();
    }

    [Fact]
    public void Can_get_unknown_keyword_that_was_set_earlier()
    {
        var builder = new RabbitMQConnectionStringBuilder
        {
            ["some"] = "other"
        };

        object? value = builder["some"];
        value.Should().Be("other");
    }
}
