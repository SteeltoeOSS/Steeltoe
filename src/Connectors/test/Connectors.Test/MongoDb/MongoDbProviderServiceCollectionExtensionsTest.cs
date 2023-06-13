// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.MongoDb;
using Xunit;

namespace Steeltoe.Connectors.Test.MongoDb;

public class MongoDbProviderServiceCollectionExtensionsTest
{
    [Fact]
    public void AddMongoClient_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddMongoClient(configurationRoot));
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddMongoClient(configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMongoClient_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddMongoClient(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddMongoClient(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMongoClient_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddMongoClient(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMongoClient_NoVCAPs_AddsMongoClient()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddMongoClient(configurationRoot);
        var service = services.BuildServiceProvider().GetService<MongoClient>();

        Assert.NotNull(service);
    }

    [Fact]
    public void AddMongoClient_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddMongoClient(configurationRoot, "foobar"));
        Assert.Contains("foobar", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMongoClient_MultipleMongoDbServices_ThrowsConnectorException()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MongoDbTestHelpers.DoubleBindingEnterpriseVcap);

        IServiceCollection services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddMongoClient(configurationRoot));
        Assert.Contains("Multiple", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMongoClient_With_Enterprise_VCAPs_AddsMongoClient()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MongoDbTestHelpers.SingleServerEnterpriseVcap);

        IServiceCollection services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddMongoClient(configurationRoot);
        var service = services.BuildServiceProvider().GetService<MongoClient>();
        var serviceByInterface = services.BuildServiceProvider().GetService<IMongoClient>();

        Assert.NotNull(service);
        Assert.NotNull(serviceByInterface);
        MongoClientSettings connSettings = service.Settings;
        Assert.Equal(28000, connSettings.Server.Port);
        Assert.Equal("192.168.12.22", connSettings.Server.Host);
        Assert.Equal("pcf_b8ce63777ce39d1c7f871f2585ba9474", connSettings.Credential.Username);
    }

    [Fact]
    public void AddMongoClient_With_a9s_single_VCAPs_AddsMongoClient()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MongoDbTestHelpers.SingleBindingA9SSingleServerVcap);

        IServiceCollection services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddMongoClient(configurationRoot);
        var service = services.BuildServiceProvider().GetService<MongoClient>();

        Assert.NotNull(service);
        MongoClientSettings connSettings = service.Settings;
        Assert.Single(connSettings.Servers);
        Assert.Equal("d8790b7-mongodb-0.node.dc1.a9s-mongodb-consul", connSettings.Server.Host);
        Assert.Equal(27017, connSettings.Server.Port);
        Assert.Equal("a9s-brk-usr-377ad48194cbf0452338737d7f6aa3fb6cdabc24", connSettings.Credential.Username);
    }

    [Fact]
    public void AddMongoClient_With_a9s_replicas_VCAPs_AddsMongoClient()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MongoDbTestHelpers.SingleBindingA9SWithReplicasVcap);

        IServiceCollection services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddMongoClient(configurationRoot);
        var service = services.BuildServiceProvider().GetService<MongoClient>();

        Assert.NotNull(service);
        MongoClientSettings connSettings = service.Settings;
        Assert.Contains(new MongoServerAddress("d5584e9-mongodb-0.node.dc1.a9s-mongodb-consul", 27017), connSettings.Servers);
        Assert.Contains(new MongoServerAddress("d5584e9-mongodb-1.node.dc1.a9s-mongodb-consul", 27017), connSettings.Servers);
        Assert.Contains(new MongoServerAddress("d5584e9-mongodb-2.node.dc1.a9s-mongodb-consul", 27017), connSettings.Servers);
        Assert.Equal("a9s-brk-usr-e74b9538ae5dcf04500eb0fc18907338d4610f30", connSettings.Credential.Username);
    }

    [Fact]
    public void AddMongoClient_With_UPS_VCAPs_AddsMongoClient()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MongoDbTestHelpers.SingleUserProvidedService);

        IServiceCollection services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddMongoClient(configurationRoot);
        var service = services.BuildServiceProvider().GetService<MongoClient>();
        var serviceByInterface = services.BuildServiceProvider().GetService<IMongoClient>();

        Assert.NotNull(service);
        Assert.NotNull(serviceByInterface);
        MongoClientSettings connSettings = service.Settings;
        Assert.Equal(28000, connSettings.Server.Port);
        Assert.Equal("host", connSettings.Server.Host);
        Assert.Equal("user", connSettings.Credential.Username);
    }

    [Fact]
    public void AddMongoClientConnection_AddsMongoDbHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddMongoClient(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as MongoDbHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddMongoClientConnection_AddingCommunityContributor_DoesNotAddSteeltoeHealthCheck()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MongoDbTestHelpers.SingleServerEnterpriseVcap);

        IServiceCollection services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<MongoDbConnectionInfo>();
        services.AddHealthChecks().AddMongoDb(ci.ConnectionString, name: ci.Name);

        services.AddMongoClient(configurationRoot, "steeltoe");
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as MongoDbHealthContributor;

        Assert.Null(healthContributor);
    }

    [Fact]
    public void AddMongoClientConnection_AddingCommunityContributor_AddsSteeltoeHealthCheckWhenForced()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", MongoDbTestHelpers.SingleServerEnterpriseVcap);

        IServiceCollection services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<MongoDbConnectionInfo>();
        services.AddHealthChecks().AddMongoDb(ci.ConnectionString, name: ci.Name);

        services.AddMongoClient(configurationRoot, "steeltoe", addSteeltoeHealthChecks: true);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as MongoDbHealthContributor;

        Assert.NotNull(healthContributor);
    }
}
