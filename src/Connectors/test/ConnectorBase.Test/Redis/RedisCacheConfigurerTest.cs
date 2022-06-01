// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using Steeltoe.Connector.Services;
using System.Net;
using Xunit;

namespace Steeltoe.Connector.Redis.Test;

[Collection("Redis")]
public class RedisCacheConfigurerTest
{
    // [Fact]
    // public void UpdateOptions_FromConfig_WithConnectionString_ReturnsExpected()
    // {
    //    RedisCacheConfigurer configurer = new RedisCacheConfigurer();
    //    RedisCacheOptions redisOptions = new RedisCacheOptions();
    //    RedisCacheConnectorOptions config = new RedisCacheConnectorOptions()
    //    {
    //        ConnectionString = "foobar",
    //        InstanceName = "instanceId"
    //    };
    //    configurer.UpdateOptions(config, redisOptions);
    //    Assert.Equal("foobar", redisOptions.Configuration);
    //    Assert.Equal("instanceId", redisOptions.InstanceName);
    // }

    // [Fact]
    // public void UpdateOptions_FromConfig_WithOutConnectionString_ReturnsExcpected()
    // {
    //    RedisCacheConfigurer configurer = new RedisCacheConfigurer();
    //    RedisCacheOptions redisOptions = new RedisCacheOptions();
    //    RedisCacheConnectorOptions config = new RedisCacheConnectorOptions()
    //    {
    //        Host = "localhost",
    //        Port = 1234,
    //        Password = "password",
    //        InstanceName = "instanceId"
    //    };
    //    configurer.UpdateOptions(config, redisOptions);
    //    Assert.Equal("localhost:1234,password=password,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", redisOptions.Configuration);
    //    Assert.Equal("instanceId", redisOptions.InstanceName);
    //// }

    [Fact]
    public void UpdateOptions_FromServiceInfo_ReturnsExcpected()
    {
        var configurer = new RedisCacheConfigurer();
        var connOptions = new RedisCacheConnectorOptions();
        var si = new RedisServiceInfo("myId", RedisServiceInfo.REDIS_SCHEME, "foobar", 4321, "sipassword");
        configurer.UpdateOptions(si, connOptions);

        Assert.Equal("foobar:4321,password=sipassword,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connOptions.ToString());
        Assert.Null(connOptions.InstanceName);
    }

    [Fact]
    public void Configure_NoServiceInfo_ReturnsExpected()
    {
        var configurer = new RedisCacheConfigurer();
        var config = new RedisCacheConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Password = "password",
            InstanceName = "instanceId"
        };
        var opts = configurer.Configure(null, config);
        Assert.NotNull(opts);
        var redisOptions = (RedisCacheOptions)opts.ToMicrosoftExtensionObject(typeof(RedisCacheOptions));
        Assert.NotNull(redisOptions);

        Assert.Equal("localhost:1234,password=password,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", redisOptions.Configuration);
        Assert.Equal("instanceId", redisOptions.InstanceName);
    }

    [Fact]
    public void Configure_ServiceInfoOveridesConfig_ReturnsExpected()
    {
        var configurer = new RedisCacheConfigurer();
        var config = new RedisCacheConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Password = "password",
            InstanceName = "instanceId"
        };
        var si = new RedisServiceInfo("myId", RedisServiceInfo.REDIS_SCHEME, "foobar", 4321, "sipassword");
        var connectionSettings = configurer.Configure(si, config);
        Assert.NotNull(connectionSettings);

        Assert.Equal("foobar:4321,password=sipassword,allowAdmin=false,abortConnect=true,resolveDns=false,ssl=false", connectionSettings.ToString());
        Assert.Equal("instanceId", connectionSettings.InstanceName);
    }

    [Fact]
    public void ConfigureConnection_NoServiceInfo_ReturnsExpected()
    {
        var configurer = new RedisCacheConfigurer();
        var config = new RedisCacheConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Password = "password"
        };

        var opts = configurer.Configure(null, config);
        Assert.NotNull(opts);

        Assert.NotNull(((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints);
        var ep = ((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints[0] as DnsEndPoint;
        Assert.NotNull(ep);
        Assert.Equal("localhost", ep.Host);
        Assert.Equal(1234, ep.Port);
        Assert.Equal("password", opts.Password);
    }

    [Fact]
    public void ConfigureConnection_ServiceInfoOveridesConfig_ReturnsExpected()
    {
        var configurer = new RedisCacheConfigurer();
        var config = new RedisCacheConnectorOptions
        {
            Host = "localhost",
            Port = 1234,
            Password = "password"
        };
        var si = new RedisServiceInfo("myId", RedisServiceInfo.REDIS_SCHEME, "foobar", 4321, "sipassword");

        var opts = configurer.Configure(si, config);
        Assert.NotNull(opts);

        Assert.NotNull(((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints);
        var ep = ((ConfigurationOptions)opts.ToStackExchangeObject(typeof(ConfigurationOptions))).EndPoints[0] as DnsEndPoint;
        Assert.NotNull(ep);
        Assert.Equal("foobar", ep.Host);
        Assert.Equal(4321, ep.Port);
        Assert.Equal("sipassword", opts.Password);
    }
}
