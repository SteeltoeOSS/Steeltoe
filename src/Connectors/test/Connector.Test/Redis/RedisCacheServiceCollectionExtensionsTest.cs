// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Connector.Redis.Test;

public class RedisCacheServiceCollectionExtensionsTest
{
    public RedisCacheServiceCollectionExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void AddDistributedRedisCache_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddDistributedRedisCache(configurationRoot));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddDistributedRedisCache(configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => services.AddDistributedRedisCache(configurationRoot, configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex3.Message);
    }

    [Fact]
    public void AddDistributedRedisCache_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;
        IConfigurationRoot connectionConfig = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddDistributedRedisCache(configuration));
        Assert.Contains(nameof(configuration), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddDistributedRedisCache(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => services.AddDistributedRedisCache(configuration, connectionConfig, "foobar"));
        Assert.Contains("applicationConfiguration", ex3.Message);
    }

    [Fact]
    public void AddDistributedRedisCache_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddDistributedRedisCache(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddDistributedRedisCache_NoVCAPs_AddsDistributedCache()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddDistributedRedisCache(configurationRoot);
        var service = services.BuildServiceProvider().GetService<IDistributedCache>();

        Assert.NotNull(service);
        Assert.IsType<RedisCache>(service);
    }

    [Fact]
    public void AddDistributedRedisCache_AddsRedisHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddDistributedRedisCache(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RedisHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddDistributedRedisCache_DoesNotAddRedisHealthContributor_WhenCommunityHealthCheckExists()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<RedisConnectionInfo>();
        services.AddHealthChecks().AddRedis(ci.ConnectionString, ci.Name);

        services.AddDistributedRedisCache(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RedisHealthContributor;

        Assert.Null(healthContributor);
    }

    [Fact]
    public void AddDistributedRedisCache_AddsRedisHealthContributor_WhenCommunityHealthCheckExistsAndForced()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<RedisConnectionInfo>();
        services.AddHealthChecks().AddRedis(ci.ConnectionString, ci.Name);

        services.AddDistributedRedisCache(configurationRoot, true);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RedisHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddDistributedRedisCache_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddDistributedRedisCache(configurationRoot, "foobar"));
        Assert.Contains("foobar", ex.Message);

        var ex2 = Assert.Throws<ConnectorException>(() => services.AddDistributedRedisCache(configurationRoot, configurationRoot, "foobar"));
        Assert.Contains("foobar", ex2.Message);
    }

    [Fact]
    public void AddDistributedRedisCache_MultipleRedisServices_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.TwoServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddDistributedRedisCache(configurationRoot));
        Assert.Contains("Multiple", ex.Message);

        var ex2 = Assert.Throws<ConnectorException>(() => services.AddDistributedRedisCache(configurationRoot, configurationRoot, null));
        Assert.Contains("Multiple", ex2.Message);
    }

    [Fact]
    public void AddRedisConnectionMultiplexer_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddRedisConnectionMultiplexer(configurationRoot));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddRedisConnectionMultiplexer(configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => services.AddRedisConnectionMultiplexer(configurationRoot, configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex3.Message);
    }

    [Fact]
    public void AddRedisConnectionMultiplexer_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;
        IConfigurationRoot connectionConfig = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddRedisConnectionMultiplexer(configuration));
        Assert.Contains(nameof(configuration), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddRedisConnectionMultiplexer(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message);

        var ex3 = Assert.Throws<ArgumentNullException>(() => services.AddRedisConnectionMultiplexer(configuration, connectionConfig, "foobar"));
        Assert.Contains("applicationConfiguration", ex3.Message);
    }

    [Fact]
    public void AddRedisConnectionMultiplexer_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddRedisConnectionMultiplexer(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddRedisConnectionMultiplexer_NoVCAPs_AddsConnectionMultiplexer()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["redis:client:host"] = "127.0.0.1",
            ["redis:client:port"] = "1234",
            ["redis:client:password"] = "pass,word",
            ["redis:client:abortOnConnectFail"] = "false",
            ["redis:client:connectTimeout"] = "1"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IServiceCollection services = new ServiceCollection();
        IServiceCollection services2 = new ServiceCollection();

        services.AddRedisConnectionMultiplexer(configurationRoot);
        var service = services.BuildServiceProvider().GetService<IConnectionMultiplexer>();

        services2.AddRedisConnectionMultiplexer(configurationRoot, configurationRoot, null);
        var service2 = services2.BuildServiceProvider().GetService<IConnectionMultiplexer>();

        Assert.NotNull(service);
        Assert.IsType<ConnectionMultiplexer>(service);
        Assert.Contains("password=pass,word", (service as ConnectionMultiplexer).Configuration);
        Assert.NotNull(service2);
        Assert.IsType<ConnectionMultiplexer>(service2);
        Assert.Contains("password=pass,word", (service as ConnectionMultiplexer).Configuration);
    }

    [Fact]
    public void AddRedisConnectionMultiplexer_AddsRedisHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddRedisConnectionMultiplexer(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RedisHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddRedisConnectionMultiplexer_WithVCAPs_AddsRedisConnectionMultiplexer()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.SingleServerVcap);

        var appsettings = new Dictionary<string, string>
        {
            ["redis:client:AbortOnConnectFail"] = "false",
            ["redis:client:connectTimeout"] = "1"
        };

        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddRedisConnectionMultiplexer(configurationRoot);
        var service = services.BuildServiceProvider().GetService<IConnectionMultiplexer>();

        Assert.NotNull(service);
        Assert.IsType<ConnectionMultiplexer>(service);
        Assert.Contains("192.168.0.103", service.Configuration);
        Assert.Contains(":60287", service.Configuration);
        Assert.Contains("password=133de7c8-9f3a-4df1-8a10-676ba7ddaa10", service.Configuration);
    }

    [Fact]
    public void AddRedisConnectionMultiplexer_WithAzureVCAPs_AddsRedisConnectionMultiplexer()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.SingleServerVcapAzureBroker);

        var appsettings = new Dictionary<string, string>
        {
            ["redis:client:AbortOnConnectFail"] = "false"
        };

        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddRedisConnectionMultiplexer(configurationRoot);
        var service = services.BuildServiceProvider().GetService<IConnectionMultiplexer>();

        Assert.NotNull(service);
        Assert.IsType<ConnectionMultiplexer>(service);
        Assert.Contains("cbe9d9a0-6502-438d-87ec-f26f1974e378.redis.cache.windows.net", service.Configuration);
        Assert.Contains(":6379", service.Configuration);
        Assert.Contains("password=V+4dv03jSUZkEcjGhVMR0hjEPfILCCcth1JE8vPRki4=", service.Configuration);
    }

    [Fact]
    public void AddRedisConnectionMultiplexer_WithEnterpriseVCAPs_AddsRedisConnectionMultiplexer()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.SingleServerEnterpriseVcap);

        var appsettings = new Dictionary<string, string>
        {
            ["redis:client:AbortOnConnectFail"] = "false",
            ["redis:client:connectTimeout"] = "1"
        };

        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddRedisConnectionMultiplexer(configurationRoot);
        var service = services.BuildServiceProvider().GetService<IConnectionMultiplexer>();

        Assert.NotNull(service);
        Assert.IsType<ConnectionMultiplexer>(service);
        Assert.Contains("redis-1076.redis-enterprise.system.cloudyazure.io", service.Configuration);
        Assert.Contains(":1076", service.Configuration);
        Assert.Contains("password=rQrMqqg-.LJzO498EcAIfp-auu4czBiGM40wjveTdHw-EJu0", service.Configuration);
    }

    [Fact]
    public void AddRedisConnectionMultiplexer_WithSecureAzureVCAPs_AddsRedisConnectionMultiplexer()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", RedisCacheTestHelpers.SingleServerVcapAzureBrokerSecure);

        var appsettings = new Dictionary<string, string>
        {
            ["redis:client:AbortOnConnectFail"] = "false"
        };

        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddRedisConnectionMultiplexer(configurationRoot);
        var service = services.BuildServiceProvider().GetService<IConnectionMultiplexer>();

        Assert.NotNull(service);
        Assert.IsType<ConnectionMultiplexer>(service);
        Assert.Contains("9b67c347-03b8-4956-aa2a-858ac30aced5.redis.cache.windows.net", service.Configuration);
        Assert.Contains(":6380", service.Configuration);
        Assert.Contains("password=mAG0+CdozukoUTOIEAo6wTKHdMoqg4+Jfno8docw3Zg=", service.Configuration);
        Assert.Contains("ssl=True", service.Configuration);
    }

    [Fact]
    public void AddRedisConnectionMultiplexer_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddRedisConnectionMultiplexer(configurationRoot, "foobar"));
        Assert.Contains("foobar", ex.Message);

        var ex2 = Assert.Throws<ConnectorException>(() => services.AddRedisConnectionMultiplexer(configurationRoot, configurationRoot, "foobar"));
        Assert.Contains("foobar", ex2.Message);
    }
}
