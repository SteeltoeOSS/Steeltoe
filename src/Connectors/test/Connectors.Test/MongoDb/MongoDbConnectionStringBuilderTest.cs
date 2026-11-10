// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using MongoDB.Driver;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using Steeltoe.Connectors.MongoDb;

namespace Steeltoe.Connectors.Test.MongoDb;

public sealed class MongoDbConnectionStringBuilderTest
{
    [Fact]
    public void Merges_properties_with_special_characters()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb://localhost:999?some=other",
            ["port"] = 123,
            ["username"] = "me@host.com",
            ["password"] = "some \"secret\" value",
            ["authenticationDatabase"] = "my db",
            ["something else"] = "another value"
        };

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
    public void Returns_null_when_getting_unknown_keyword()
    {
        var builder = new MongoDbConnectionStringBuilder();

        object? some = builder["some"];

        some.Should().BeNull();
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

    [Theory]
    [InlineData("mongodb+srv://user:pass@cluster0.example.net/admin?retryWrites=true", "mongodb+srv://user:pass@cluster0.example.net/admin?retryWrites=true")]
    [InlineData("mongodb+srv://cluster0.example.net", "mongodb+srv://cluster0.example.net/")]
    public void Preserves_SRV_scheme(string connectionString, string expected)
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        builder.ConnectionString.Should().Be(expected);
        builder["scheme"].Should().Be("mongodb+srv");
        builder["server"].Should().Be("cluster0.example.net");

        new MongoUrl(builder.ConnectionString).Scheme.Should().Be(ConnectionStringScheme.MongoDBPlusSrv);
    }

    [Fact]
    public void Merges_properties_into_SRV_connection_string()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb+srv://cluster0.example.net/?retryWrites=true",
            ["username"] = "me",
            ["password"] = "secret"
        };

        builder.ConnectionString.Should().Be("mongodb+srv://me:secret@cluster0.example.net/?retryWrites=true");
    }

    [Theory]
    [InlineData("port", "27017", "mongodb://cluster0.example.net:27017/")]
    [InlineData("server", "other.example.net", "mongodb://other.example.net/")]
    public void Falls_back_to_default_scheme_when_overriding_server_or_port_of_SRV_connection_string(string keyword, string value, string expected)
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb+srv://cluster0.example.net",
            [keyword] = value
        };

        builder["scheme"].Should().Be("mongodb");
        builder.ConnectionString.Should().Be(expected);
    }

    [Fact]
    public void Can_set_scheme()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb://user:pass@cluster0.example.net/admin?retryWrites=true",
            ["scheme"] = "mongodb+srv"
        };

        builder.ConnectionString.Should().Be("mongodb+srv://user:pass@cluster0.example.net/admin?retryWrites=true");
        builder["scheme"].Should().Be("mongodb+srv");
    }

    [Fact]
    public void Resets_scheme_when_replacing_connection_string()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb+srv://cluster0.example.net",
            ["url"] = "mongodb://localhost:123"
        };

        builder.ConnectionString.Should().Be("mongodb://localhost:123/");

        builder.ConnectionString = null;
        builder["scheme"].Should().BeNull();
        builder["server"] = "localhost";

        builder.ConnectionString.Should().Be("mongodb://localhost/");
    }

    [Theory]
    [InlineData("mongodb://host1:27017,host2:27017")]
    [InlineData("mongodb://user:pass@host1,host2/db")]
    [InlineData("mongodb://user:pass@host1,host2?replicaSet=rs0")]
    [InlineData("mongodb+srv://host1,host2")]
    public void Throws_on_multiple_servers(string connectionString)
    {
        var builder = new MongoDbConnectionStringBuilder();

        Action action = () => builder.ConnectionString = connectionString;

        action.Should().ThrowExactly<NotImplementedException>();
    }

    [Fact]
    public void Allows_commas_in_password_and_query_string()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb://user:pa,ss@localhost:27017/db?compressors=zstd,snappy&readPreference=secondary&readPreferenceTags=dc:east,rack:1"
        };

        builder["server"].Should().Be("localhost");
        builder["password"].Should().Be("pa,ss");
        builder["compressors"].Should().Be("zstd,snappy");
        builder["readPreferenceTags"].Should().Be("dc:east,rack:1");

        var mongoUrl = new MongoUrl(builder.ConnectionString);

        mongoUrl.Password.Should().Be("pa,ss");
        mongoUrl.Compressors.Select(compressor => compressor.Type).Should().Equal(CompressorType.ZStandard, CompressorType.Snappy);
    }

    [Fact]
    public void Treats_semicolon_as_separator_only_in_query_string()
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = "mongodb://user:pa;ss@localhost:27017/db?first=one;second=two"
        };

        builder["password"].Should().Be("pa;ss");
        builder["first"].Should().Be("one");
        builder["second"].Should().Be("two");
    }
}
