// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Services.Test;

public class UriInfoTest
{
    [Fact]
    public void Constructor_Uri()
    {
        string uri = "mysql://joe:joes_password@localhost:1527/big_db";
        var result = new UriInfo(uri);

        AssertUriInfoEquals(result, "localhost", 1527, "joe", "joes_password", "big_db", null);
        Assert.Equal(uri, result.UriString);
    }

    [Fact]
    public void Constructor_WithQuery()
    {
        string uri = "mysql://joe:joes_password@localhost:1527/big_db?p1=v1&p2=v2";
        var result = new UriInfo(uri);

        AssertUriInfoEquals(result, "localhost", 1527, "joe", "joes_password", "big_db", "p1=v1&p2=v2");
        Assert.Equal(uri, result.UriString);
    }

    [Fact]
    public void Constructor_NoUsernamePassword()
    {
        string uri = "mysql://localhost:1527/big_db";
        var result = new UriInfo(uri);

        AssertUriInfoEquals(result, "localhost", 1527, null, null, "big_db", null);
        Assert.Equal(uri, result.UriString);
    }

    [Fact]
    public void Constructor_WithUsernameNoPassword()
    {
        string uri = "mysql://joe@localhost:1527/big_db";
        var ex = Assert.Throws<ArgumentException>(() => new UriInfo(uri));
        Assert.Contains("joe", ex.Message);
    }

    [Fact]
    public void Constructor_WithExplicitParameters()
    {
        string uri = "mysql://joe:joes_password@localhost:1527/big_db";
        var result = new UriInfo("mysql", "localhost", 1527, "joe", "joes_password", "big_db");

        AssertUriInfoEquals(result, "localhost", 1527, "joe", "joes_password", "big_db", null);
        Assert.Equal(uri, result.UriString);
    }

    private void AssertUriInfoEquals(UriInfo result, string host, int port, string username, string password, string path, string query)
    {
        Assert.Equal(host, result.Host);
        Assert.Equal(port, result.Port);
        Assert.Equal(username, result.UserName);
        Assert.Equal(password, result.Password);
        Assert.Equal(path, result.Path);
        Assert.Equal(query, result.Query);
    }
}
