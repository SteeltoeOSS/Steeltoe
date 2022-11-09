// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Configuration;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.Test.Services;

public class Db2ServiceInfoFactoryTest
{
    [Fact]
    public void Accept_AcceptsValidServiceBinding()
    {
        var s = new Service
        {
            Label = "p-db2",
            Tags = new[]
            {
                "db2",
                "relational"
            },
            Name = "db2Service",
            Plan = "100mb-dev",
            Credentials =
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("3306") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("db2://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                {
                    "jdbcUrl",
                    new Credential("jdbc:db2://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt")
                }
            }
        };

        var factory = new Db2ServiceInfoFactory();
        Assert.True(factory.Accepts(s));
    }

    [Fact]
    public void Accept_RejectsInvalidServiceBinding()
    {
        var s = new Service
        {
            Label = "p-msql",
            Tags = new[]
            {
                "foobar",
                "relational"
            },
            Name = "mysqlService",
            Plan = "100mb-dev",
            Credentials =
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("3306") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                {
                    "jdbcUrl",
                    new Credential("jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt")
                }
            }
        };

        var factory = new Db2ServiceInfoFactory();
        Assert.False(factory.Accepts(s));
    }

    [Fact]
    public void Create_CreatesValidServiceBinding()
    {
        var s = new Service
        {
            Label = "p-db2",
            Tags = new[]
            {
                "db2",
                "relational"
            },
            Name = "db2Service",
            Plan = "100mb-dev",
            Credentials =
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("3306") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("db2://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                {
                    "jdbcUrl",
                    new Credential("jdbc:db2://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt")
                }
            }
        };

        var factory = new Db2ServiceInfoFactory();
        var info = factory.Create(s) as Db2ServiceInfo;
        Assert.NotNull(info);
        Assert.Equal("db2Service", info.Id);
        Assert.Equal("7E1LxXnlH2hhlPVt", info.Password);
        Assert.Equal("Dd6O1BPXUHdrmzbP", info.UserName);
        Assert.Equal("192.168.0.90", info.Host);
        Assert.Equal(3306, info.Port);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", info.Path);
        Assert.Equal("reconnect=true", info.Query);
    }
}
