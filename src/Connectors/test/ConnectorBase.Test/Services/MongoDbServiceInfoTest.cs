// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Services.Test;

public class MongoDbServiceInfoTest
{
    [Fact]
    public void Constructor_CreatesExpected()
    {
        var uri = "mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355";
        var r1 = new MongoDbServiceInfo("myId", uri);
        var r2 = new MongoDbServiceInfo("myId", "192.168.0.90", 27017, "Dd6O1BPXUHdrmzbP", "7E1LxXnlH2hhlPVt", "cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355");

        Assert.Equal("myId", r1.Id);
        Assert.Equal("mongodb", r1.Scheme);
        Assert.Equal("192.168.0.90", r1.Host);
        Assert.Equal(27017, r1.Port);
        Assert.Equal("7E1LxXnlH2hhlPVt", r1.Password);
        Assert.Equal("Dd6O1BPXUHdrmzbP", r1.UserName);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", r1.Path);
        Assert.Null(r1.Query);

        Assert.Equal("myId", r2.Id);
        Assert.Equal("mongodb", r2.Scheme);
        Assert.Equal("192.168.0.90", r2.Host);
        Assert.Equal(27017, r2.Port);
        Assert.Equal("7E1LxXnlH2hhlPVt", r2.Password);
        Assert.Equal("Dd6O1BPXUHdrmzbP", r2.UserName);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", r2.Path);
        Assert.Null(r2.Query);
    }

    [Fact]
    public void Constructor_HandlesReplicas()
    {
        var uri = "mongodb://a9s-brk-usr-e74b9538:a9sb8b69cc@mongodb-0.node.dc1.a9s-mongodb-consul:27017,mongodb-1.node.dc1.a9s-mongodb-consul:27017,mongodb-2.node.dc1.a9s-mongodb-consul:27017/d5584e9?replicaSet=rs0";
        var r1 = new MongoDbServiceInfo("myId", uri);

        Assert.Equal("myId", r1.Id);
        Assert.Equal("mongodb", r1.Scheme);
        Assert.Contains("mongodb-0.node.dc1.a9s-mongodb-consul:27017", r1.Hosts);
        Assert.Contains("mongodb-1.node.dc1.a9s-mongodb-consul:27017", r1.Hosts);
        Assert.Contains("mongodb-2.node.dc1.a9s-mongodb-consul:27017", r1.Hosts);
        Assert.Equal("a9sb8b69cc", r1.Password);
        Assert.Equal("a9s-brk-usr-e74b9538", r1.UserName);
        Assert.Equal("d5584e9", r1.Path);
        Assert.Equal("replicaSet=rs0", r1.Query);
    }
}
