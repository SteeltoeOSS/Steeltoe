// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connectors.RabbitMQ;

namespace Steeltoe.Connectors.Test.RabbitMQ;

public sealed class RabbitMQConnectionStringBuilderTests
{
    [Fact]
    public void Merges_properties_with_special_characters()
    {
        var builder = new RabbitMQConnectionStringBuilder
        {
            ConnectionString = "amqps://localhost:999/virtual-host-1"
        };

        builder["useTls"] = false;
        builder["port"] = 123;
        builder["username"] = "me@host.com";
        builder["password"] = "some \"secret\" value";
        builder["virtualHost"] = "my virtual= host";

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
    public void Returns_null_when_getting_known_keyword()
    {
        var builder = new RabbitMQConnectionStringBuilder();

        object? port = builder["port"];

        port.Should().BeNull();
    }

    [Fact]
    public void Throws_when_getting_unknown_keyword()
    {
        var builder = new RabbitMQConnectionStringBuilder();

        Action action = () => _ = builder["bad"];

        action.Should().ThrowExactly<ArgumentException>().WithMessage("Keyword not supported: 'bad'.*");
    }

    [Fact]
    public void Throws_when_setting_unknown_keyword()
    {
        var builder = new RabbitMQConnectionStringBuilder();

        Action action = () => builder["bad"] = "some";

        action.Should().ThrowExactly<ArgumentException>().WithMessage("Keyword not supported: 'bad'.*");
    }
}
