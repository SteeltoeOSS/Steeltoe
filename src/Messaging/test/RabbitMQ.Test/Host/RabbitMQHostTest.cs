// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Host;

public class RabbitMQHostTest
{
    [Fact]
    public void HostCanBeStarted()
    {
        MockRabbitHostedService hostedService;

        using (IHost host = RabbitMQHost.CreateDefaultBuilder().ConfigureServices(svc => svc.AddSingleton<IHostedService, MockRabbitHostedService>()).Start())
        {
            Assert.NotNull(host);
            hostedService = (MockRabbitHostedService)host.Services.GetRequiredService<IHostedService>();
            Assert.NotNull(hostedService);
            Assert.Equal(1, hostedService.StartCount);
            Assert.Equal(0, hostedService.StopCount);
            Assert.Equal(0, hostedService.DisposeCount);
        }

        Assert.Equal(1, hostedService.StartCount);
        Assert.Equal(0, hostedService.StopCount);
        Assert.Equal(1, hostedService.DisposeCount);
    }

    [Fact]
    public void HostShouldInitializeServices()
    {
        using IHost host = RabbitMQHost.CreateDefaultBuilder().Start();
        var lifecycleProcessor = host.Services.GetRequiredService<ILifecycleProcessor>();
        var rabbitHostService = (RabbitHostService)host.Services.GetRequiredService<IHostedService>();

        Assert.True(lifecycleProcessor.IsRunning);
        Assert.NotNull(rabbitHostService);
    }

    [Fact]
    public void HostShouldAddRabbitOptionsConfiguration()
    {
        IHostBuilder hostBuilder = RabbitMQHost.CreateDefaultBuilder();

        var appSettings = new Dictionary<string, string>
        {
            [$"{RabbitOptions.Prefix}:host"] = "ThisIsATest",
            [$"{RabbitOptions.Prefix}:port"] = "1234",
            [$"{RabbitOptions.Prefix}:username"] = "TestUser",
            [$"{RabbitOptions.Prefix}:password"] = "TestPassword"
        };

        hostBuilder.ConfigureAppConfiguration(configBuilder =>
        {
            configBuilder.AddInMemoryCollection(appSettings);
        });

        using IHost host = hostBuilder.Start();
        RabbitOptions rabbitOptions = host.Services.GetService<IOptions<RabbitOptions>>()?.Value;

        Assert.NotNull(rabbitOptions);
        Assert.Equal("ThisIsATest", rabbitOptions.Host);
        Assert.Equal(1234, rabbitOptions.Port);
        Assert.Equal("TestUser", rabbitOptions.Username);
        Assert.Equal("TestPassword", rabbitOptions.Password);
    }

    [Fact]
    public void HostShouldSendCommandLineArgs()
    {
        IHostBuilder hostBuilder = RabbitMQHost.CreateDefaultBuilder(new[]
        {
            "RabbitHostCommandKey=RabbitHostCommandValue"
        });

        using IHost host = hostBuilder.Start();
        var configuration = host.Services.GetService<IConfiguration>();

        Assert.Equal("RabbitHostCommandValue", configuration["RabbitHostCommandKey"]);
    }

    [Fact]
    public void ShouldWorkWithRabbitMQConnection()
    {
        using IHost host = RabbitMQHost.CreateDefaultBuilder().ConfigureServices(svc => svc.AddRabbitMQConnection(new ConfigurationBuilder().Build())).Start();
        var connectionFactory = host.Services.GetRequiredService<RC.IConnectionFactory>();

        Assert.NotNull(connectionFactory);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    [Trait("Category", "SkipOnLinux")]
    public void HostConfiguresRabbitOptions()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", TestConfiguration.CloudFoundryRabbitMqConfiguration);
        using IHost host = RabbitMQHost.CreateDefaultBuilder().ConfigureAppConfiguration(c => c.AddCloudFoundry()).Start();
        var rabbitOptionsMonitor = host.Services.GetService<IOptionsMonitor<RabbitOptions>>();
        Assert.NotNull(rabbitOptionsMonitor);
        RabbitOptions rabbitOptions = rabbitOptionsMonitor.CurrentValue;

        Assert.Equal("Dd6O1BPXUHdrmzbP", rabbitOptions.Username);
        Assert.Equal("7E1LxXnlH2hhlPVt", rabbitOptions.Password);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", rabbitOptions.VirtualHost);
        Assert.Equal("Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306", rabbitOptions.Addresses);
    }
}
