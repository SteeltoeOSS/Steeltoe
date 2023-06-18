// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using Steeltoe.Connectors.Redis;
using Steeltoe.Connectors.Services;
using Xunit;

namespace Steeltoe.Connectors.Test.Redis;

[Collection("Redis")]
public class RedisCacheConfigurerTest
{
    [Fact]
    public void UpdateOptions_FromServiceInfo_ReturnsExpected()
    {
        var configurer = new RedisCacheConfigurer();
        var options = new RedisCacheConnectorOptions();
        var si = new RedisServiceInfo("myId", RedisServiceInfo.RedisScheme, "foobar", 4321, "sipassword");
        configurer.UpdateOptions(si, options);

        Assert.Equal("foobar:4321,password=sipassword,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", options.ToString());
        Assert.Null(options.InstanceName);
    }

    [Fact]
    public void Configure_NoServiceInfo_ReturnsExpected()
    {
        var configurer = new RedisCacheConfigurer();

        var options = new RedisCacheConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Password = "password",
            InstanceName = "instanceId"
        };

        RedisCacheConnectorOptions opts = configurer.Configure(null, options);
        Assert.NotNull(opts);
        var redisOptions = (RedisCacheOptions)opts.ToMicrosoftExtensionObject(typeof(RedisCacheOptions));
        Assert.NotNull(redisOptions);

        Assert.Equal("localhost:1234,password=password,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", redisOptions.Configuration);
        Assert.Equal("instanceId", redisOptions.InstanceName);
    }

    [Fact]
    public void Configure_ServiceInfoOverridesConfig_ReturnsExpected()
    {
        var configurer = new RedisCacheConfigurer();

        var options = new RedisCacheConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Password = "password",
            InstanceName = "instanceId"
        };

        var si = new RedisServiceInfo("myId", RedisServiceInfo.RedisScheme, "foobar", 4321, "sipassword");
        RedisCacheConnectorOptions connectionSettings = configurer.Configure(si, options);
        Assert.NotNull(connectionSettings);

        Assert.Equal("foobar:4321,password=sipassword,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connectionSettings.ToString());
        Assert.Equal("instanceId", connectionSettings.InstanceName);
    }

    [Fact]
    public void ConfigureConnection_NoServiceInfo_ReturnsExpected()
    {
        var configurer = new RedisCacheConfigurer();

        var options = new RedisCacheConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Password = "password"
        };

        RedisCacheConnectorOptions opts = configurer.Configure(null, options);
        Assert.NotNull(opts);

        Assert.NotNull(((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints);
        var ep = ((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints[0] as DnsEndPoint;
        Assert.NotNull(ep);
        Assert.Equal("localhost", ep.Host);
        Assert.Equal(1234, ep.Port);
        Assert.Equal("password", opts.Password);
    }

    [Fact]
    public void ConfigureConnection_ServiceInfoOverridesConfig_ReturnsExpected()
    {
        var configurer = new RedisCacheConfigurer();

        var options = new RedisCacheConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Password = "password"
        };

        var si = new RedisServiceInfo("myId", RedisServiceInfo.RedisScheme, "foobar", 4321, "sipassword");

        RedisCacheConnectorOptions opts = configurer.Configure(si, options);
        Assert.NotNull(opts);

        Assert.NotNull(((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints);
        var ep = ((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints[0] as DnsEndPoint;
        Assert.NotNull(ep);
        Assert.Equal("foobar", ep.Host);
        Assert.Equal(4321, ep.Port);
        Assert.Equal("sipassword", opts.Password);
    }
}
