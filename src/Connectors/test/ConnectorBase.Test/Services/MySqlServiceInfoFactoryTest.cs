﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Connector.Services.Test;

public class MySqlServiceInfoFactoryTest
{
    [Fact]
    public void Accept_AcceptsValidServiceBinding()
    {
        var s = new Service()
        {
            Label = "p-mysql",
            Tags = new string[] { "mysql", "relational" },
            Name = "mySqlService",
            Plan = "100mb-dev",
            Credentials = new Credential()
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("3306") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                { "jdbcUrl", new Credential("jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
            }
        };
        var factory = new MySqlServiceInfoFactory();
        Assert.True(factory.Accepts(s));
    }

    [Fact]
    public void Accept_AcceptsNoLabelNoTagsServiceBinding()
    {
        var s = new Service()
        {
            Name = "mySqlService",
            Credentials = new Credential()
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("3306") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                { "jdbcUrl", new Credential("jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
            }
        };
        var factory = new MySqlServiceInfoFactory();
        Assert.True(factory.Accepts(s));
    }

    [Fact]
    public void Accept_AcceptsLabelNoTagsServiceBinding()
    {
        var s = new Service()
        {
            Label = "p-mysql",
            Tags = System.Array.Empty<string>(),
            Name = "mySqlService",
            Plan = "100mb-dev",
            Credentials = new Credential()
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("3306") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                { "jdbcUrl", new Credential("jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
            }
        };
        var factory = new MySqlServiceInfoFactory();
        Assert.True(factory.Accepts(s));
    }

    [Fact]
    public void Accept_RejectsInvalidServiceBinding()
    {
        var s = new Service()
        {
            Label = "p-foobar",
            Tags = new string[] { "foobar", "relational" },
            Name = "mySqlService",
            Plan = "100mb-dev",
            Credentials = new Credential()
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("3306") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("foobar://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                { "jdbcUrl", new Credential("jdbc:foobar://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
            }
        };
        var factory = new MySqlServiceInfoFactory();
        Assert.False(factory.Accepts(s));
    }

    [Fact]
    public void Create_CreatesValidServiceBinding()
    {
        var s = new Service()
        {
            Label = "p-mysql",
            Tags = new string[] { "mysql", "relational" },
            Name = "mySqlService",
            Plan = "100mb-dev",
            Credentials = new Credential()
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("3306") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
                { "jdbcUrl", new Credential("jdbc:mysql://192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?user=Dd6O1BPXUHdrmzbP&password=7E1LxXnlH2hhlPVt") }
            }
        };
        var factory = new MySqlServiceInfoFactory();
        var info = factory.Create(s) as MySqlServiceInfo;
        Assert.NotNull(info);
        Assert.Equal("mySqlService", info.Id);
        Assert.Equal("7E1LxXnlH2hhlPVt", info.Password);
        Assert.Equal("Dd6O1BPXUHdrmzbP", info.UserName);
        Assert.Equal("192.168.0.90", info.Host);
        Assert.Equal(3306, info.Port);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", info.Path);
        Assert.Equal("reconnect=true", info.Query);
    }

    [Fact]
    public void Create_CreatesValidServiceBinding_NoUri()
    {
        var s = new Service()
        {
            Label = "p-mysql",
            Tags = new string[] { "mysql", "relational" },
            Name = "mySqlService",
            Plan = "100mb-dev",
            Credentials = new Credential()
            {
                { "hostname", new Credential("192.168.0.90") },
                { "port", new Credential("3306") },
                { "name", new Credential("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355") },
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") }
            }
        };
        var factory = new MySqlServiceInfoFactory();
        var info = factory.Create(s) as MySqlServiceInfo;
        Assert.NotNull(info);
        Assert.Equal("mySqlService", info.Id);
        Assert.Equal("7E1LxXnlH2hhlPVt", info.Password);
        Assert.Equal("Dd6O1BPXUHdrmzbP", info.UserName);
        Assert.Equal("192.168.0.90", info.Host);
        Assert.Equal(3306, info.Port);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", info.Path);
    }

    [Fact]
    public void Accept_AcceptsValidCupsServiceBinding()
    {
        var s = new Service()
        {
            Label = "user-provided",
            Tags = System.Array.Empty<string>(),
            Name = "cupsMySqlService",
            Credentials = new Credential()
            {
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
            }
        };
        var factory = new MySqlServiceInfoFactory();
        Assert.True(factory.Accepts(s));
    }

    [Fact]
    public void Create_CreatesValidServiceBinding_Cups()
    {
        var s = new Service()
        {
            Label = "user-provided",
            Tags = System.Array.Empty<string>(),
            Name = "cupsMySqlService",
            Credentials = new Credential()
            {
                { "username", new Credential("Dd6O1BPXUHdrmzbP") },
                { "password", new Credential("7E1LxXnlH2hhlPVt") },
                { "uri", new Credential("mysql://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355?reconnect=true") },
            }
        };
        var factory = new MySqlServiceInfoFactory();
        var info = factory.Create(s) as MySqlServiceInfo;
        Assert.NotNull(info);
        Assert.Equal("cupsMySqlService", info.Id);
        Assert.Equal("7E1LxXnlH2hhlPVt", info.Password);
        Assert.Equal("Dd6O1BPXUHdrmzbP", info.UserName);
        Assert.Equal("192.168.0.90", info.Host);
        Assert.Equal(3306, info.Port);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", info.Path);
    }
}