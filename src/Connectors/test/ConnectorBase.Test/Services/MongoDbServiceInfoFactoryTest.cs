// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Connector.Services.Test;

public class MongoDbServiceInfoFactoryTest
{
    [Fact]
    public void Accept_AcceptsValidServiceBinding()
    {
        var s = new Service
        {
            Label = "p-mongodb",
            Tags = new[]
            {
                "mongodb"
            },
            Name = "mongoService",
            Plan = "free",
            Credentials = new Credential
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("27017") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                {
                    "uri",
                    new Credential("mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true")
                }
            }
        };

        var factory = new MongoDbServiceInfoFactory();
        Assert.True(factory.Accepts(s));
    }

    [Fact]
    public void Accept_AcceptsNoLabelNoTagsServiceBinding()
    {
        var s = new Service
        {
            Label = string.Empty,
            Tags = Array.Empty<string>(),
            Name = "mongoService",
            Plan = "free",
            Credentials = new Credential
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("27017") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                {
                    "uri",
                    new Credential("mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true")
                }
            }
        };

        var factory = new MongoDbServiceInfoFactory();
        Assert.True(factory.Accepts(s));
    }

    [Fact]
    public void Accept_RejectsInvalidServiceBinding()
    {
        var s = new Service
        {
            Label = "p-mysql",
            Tags = new[]
            {
                "foobar",
                "relational"
            },
            Name = "mySqlService",
            Plan = "100mb-dev",
            Credentials = new Credential
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("27017") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                {
                    "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true")
                },
                {
                    "jdbcUrl",
                    new Credential("jdbc:mysql://192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt")
                }
            }
        };

        var factory = new MongoDbServiceInfoFactory();
        Assert.False(factory.Accepts(s));
    }

    [Fact]
    public void Create_CreatesValidServiceBinding()
    {
        var s = new Service
        {
            Label = "p-mongodb",
            Tags = new[]
            {
                "mongodb"
            },
            Name = "mongodbService",
            Plan = "free",
            Credentials = new Credential
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("27017") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("mongodb://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:27017/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") }
            }
        };

        var factory = new MongoDbServiceInfoFactory();
        var info = factory.Create(s) as MongoDbServiceInfo;
        Assert.NotNull(info);
        Assert.Equal("mongodbService", info.Id);
        Assert.Equal("7E1LxXnlH2hhlPVt", info.Password);
        Assert.Equal("Dd6O1BPXUHdrmzbP", info.UserName);
        Assert.Equal("192.168.0.90", info.Host);
        Assert.Equal(27017, info.Port);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", info.Path);
    }
}
