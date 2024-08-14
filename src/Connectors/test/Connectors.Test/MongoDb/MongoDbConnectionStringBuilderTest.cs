// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connectors.MongoDb;

namespace Steeltoe.Connectors.Test.MongoDb;

public sealed class MongoDbConnectionStringBuilderTest
{
    [Fact]
    public void Merges_properties_with_special_characters()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb://localhost:999?some=other"
        };

        builder["port"] = 123;
        builder["username"] = "me@host.com";
        builder["password"] = "some \"secret\" value";
        builder["authenticationDatabase"] = "my db";
        builder["something else"] = "another value";

        builder.ConnectionString.Should().Be(
            "mongodb://me%40host.com:some%20%22secret%22%20value@localhost:123/my%20db?some=other&something%20else=another%20value");

        builder["URL"].Should().Be(builder.ConnectionString);
    }

    [Fact]
    public void Decodes_properties_with_special_characters()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb://me%40host.com:some%20%22secret%22%20value@localhost:123/my%20db?some%3f=other%3D"
        };

        builder["server"].Should().Be("localhost");
        builder["port"].Should().Be("123");
        builder["username"].Should().Be("me@host.com");
        builder["password"].Should().Be("some \"secret\" value");
        builder["authenticationDatabase"].Should().Be("my db");
        builder["some?"].Should().Be("other=");
    }

    [Fact]
    public void Preserves_query_string_parameters_when_setting_URL()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb://user:pass@myhost?first=one&second=two"
        };

        const string url = "mongodb://localhost:123?first=one&second=number2";
        builder["url"] = url;

        builder.ConnectionString.Should().Be("mongodb://localhost:123/?first=one&second=number2");
        builder["url"].Should().Be(builder.ConnectionString);
    }

    [Fact]
    public void Splits_query_string_parameters_on_semicolon()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb://user:pass@myhost?first=one;second=two&third=three"
        };

        builder["first"].Should().Be("one");
        builder["second"].Should().Be("two");
        builder["third"].Should().Be("three");
    }

    [Fact]
    public void Returns_null_when_getting_known_keyword()
    {
        var builder = new MongoDbConnectionStringBuilder();

        object? port = builder["port"];

        port.Should().BeNull();
    }

    [Fact]
    public void Throws_when_getting_unknown_keyword()
    {
        var builder = new MongoDbConnectionStringBuilder();

        Action action = () => _ = builder["bad"];

        action.Should().ThrowExactly<ArgumentException>().WithMessage("Keyword not supported: 'bad'.*");
    }

    [Fact]
    public void Can_get_unknown_keyword_that_was_set_earlier()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ["some"] = "other"
        };

        object? value = builder["some"];
        value.Should().Be("other");
    }
}
