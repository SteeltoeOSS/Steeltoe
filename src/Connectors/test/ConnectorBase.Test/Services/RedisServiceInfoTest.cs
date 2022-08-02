// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Services.Test;

public class RedisServiceInfoTest
{
    [Fact]
    public void Constructor_CreatesExpected()
    {
        string uri = "redis://joe:joes_password@localhost:1527/";
        var r1 = new RedisServiceInfo("myId", RedisServiceInfo.RedisScheme, "localhost", 1527, "joes_password");
        var r2 = new RedisServiceInfo("myId", uri);

        Assert.Equal("myId", r1.Id);
        Assert.Equal("redis", r1.Scheme);
        Assert.Equal("localhost", r1.Host);
        Assert.Equal(1527, r1.Port);
        Assert.Equal("joes_password", r1.Password);
        Assert.Null(r1.Path);
        Assert.Null(r1.Query);

        Assert.Equal("myId", r2.Id);
        Assert.Equal("redis", r2.Scheme);
        Assert.Equal("localhost", r2.Host);
        Assert.Equal(1527, r2.Port);
        Assert.Equal("joe", r2.UserName);
        Assert.Equal("joes_password", r2.Password);
        Assert.Equal(string.Empty, r2.Path);
        Assert.Null(r2.Query);
    }

    [Fact]
    public void Constructor_CreatesExpected_withSecure()
    {
        string uri = "rediss://:joes_password@localhost:6380/";
        var r1 = new RedisServiceInfo("myId", RedisServiceInfo.RedisScheme, "localhost", 1527, "joes_password");
        var r2 = new RedisServiceInfo("myId", uri);

        Assert.Equal("myId", r1.Id);
        Assert.Equal("redis", r1.Scheme);
        Assert.Equal("localhost", r1.Host);
        Assert.Equal(1527, r1.Port);
        Assert.Equal("joes_password", r1.Password);
        Assert.Null(r1.Path);
        Assert.Null(r1.Query);

        Assert.Equal("myId", r2.Id);
        Assert.Equal("rediss", r2.Scheme);
        Assert.Equal("localhost", r2.Host);
        Assert.Equal(6380, r2.Port);
        Assert.Equal("joes_password", r2.Password);
        Assert.Equal(string.Empty, r2.Path);
        Assert.Null(r2.Query);
    }

    [Theory]
    [InlineData("redis")]
    [InlineData("rediss")]
    public void Constructor_CreatesExpected_WithSchema(string scheme)
    {
        _ = $"{scheme}://:joes_password@localhost:6380/";
        var redisInfo = new RedisServiceInfo("myId", scheme, "localhost", 1527, "joes_password");

        Assert.Equal("myId", redisInfo.Id);
        Assert.Equal(scheme, redisInfo.Scheme);
        Assert.Equal("localhost", redisInfo.Host);
        Assert.Equal(1527, redisInfo.Port);
        Assert.Equal("joes_password", redisInfo.Password);
        Assert.Null(redisInfo.Path);
        Assert.Null(redisInfo.Query);
    }
}
