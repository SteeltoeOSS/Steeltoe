// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Services.Test;

public class UriServiceInfoTest
{
    [Fact]
    public void Constructor_CreatesExpected()
    {
        const string uri = "mysql://joe:joes_password@localhost:1527/big_db";
        UriServiceInfo r1 = new TestUriServiceInfo("myId", "mysql", "localhost", 1527, "joe", "joes_password", "big_db");
        UriServiceInfo r2 = new TestUriServiceInfo("myId", uri);

        Assert.Equal("myId", r1.Id);
        Assert.Equal("mysql", r1.Scheme);
        Assert.Equal("localhost", r1.Host);
        Assert.Equal(1527, r1.Port);
        Assert.Equal("joe", r1.UserName);
        Assert.Equal("joes_password", r1.Password);
        Assert.Equal("big_db", r1.Path);
        Assert.Null(r1.Query);

        Assert.Equal("myId", r2.Id);
        Assert.Equal("mysql", r2.Scheme);
        Assert.Equal("localhost", r2.Host);
        Assert.Equal(1527, r2.Port);
        Assert.Equal("joe", r2.UserName);
        Assert.Equal("joes_password", r2.Password);
        Assert.Equal("big_db", r2.Path);
        Assert.Null(r2.Query);
    }

    [Fact]
    public void UriEncodingIsApplied()
    {
        UriServiceInfo r1 = new TestUriServiceInfo("myId", "http", "foo.bar", 1337, "SomeUser", "P4SSW0RD#", "baz");

        Assert.Equal("myId", r1.Id);
        Assert.Equal("http", r1.Scheme);
        Assert.Equal("foo.bar", r1.Host);
        Assert.Equal(1337, r1.Port);
        Assert.Equal("SomeUser", r1.UserName);
        Assert.Equal("P4SSW0RD#", r1.Password);
        Assert.Equal("baz", r1.Path);
        Assert.Null(r1.Query);
    }
}
