// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Host;

public class RabbitMQHostWebApplicationTest
{
    [Fact]
    public void WebAppHostCanBeStarted()
    {
        MockRabbitHostedService hostedService;

        WebApplicationBuilder builder = RabbitMQHost.CreateWebApplicationBuilder();
        builder.Services.AddSingleton<IHostedService, MockRabbitHostedService>();

        using (WebApplication webApp = builder.Build())
        {
            webApp.Start();
            hostedService = webApp.Services.GetServices<IHostedService>().OfType<MockRabbitHostedService>().First();
            Assert.NotNull(hostedService);
            Assert.Equal(1, hostedService.StartCount);
            Assert.Equal(0, hostedService.StopCount);
            Assert.Equal(0, hostedService.DisposeCount);
            webApp.StopAsync().Wait();
        }

        Assert.Equal(1, hostedService.StartCount);
        Assert.Equal(1, hostedService.StopCount);
        Assert.Equal(1, hostedService.DisposeCount);
    }

    [Fact]
    public void WebAppHostShouldInitializeServices()
    {
        using WebApplication webApp = RabbitMQHost.CreateWebApplicationBuilder().Build();
        webApp.Start();
        var lifecycleProcessor = webApp.Services.GetRequiredService<ILifecycleProcessor>();
        RabbitHostService rabbitHostService = webApp.Services.GetServices<IHostedService>().OfType<RabbitHostService>().First();

        Assert.True(lifecycleProcessor.IsRunning);
        Assert.NotNull(rabbitHostService);
        webApp.StopAsync();
    }

    [Fact]
    public void WebAppHostShouldAddRabbitOptionsConfiguration()
    {
        WebApplicationBuilder builder = RabbitMQHost.CreateWebApplicationBuilder();

        var appSettings = new Dictionary<string, string>
        {
            [$"{RabbitOptions.Prefix}:host"] = "ThisIsATest",
            [$"{RabbitOptions.Prefix}:port"] = "1234",
            [$"{RabbitOptions.Prefix}:username"] = "TestUser",
            [$"{RabbitOptions.Prefix}:password"] = "TestPassword"
        };

        builder.Configuration.AddInMemoryCollection(appSettings);

        using WebApplication webApp = builder.Build();
        RabbitOptions rabbitOptions = webApp.Services.GetService<IOptions<RabbitOptions>>()?.Value;

        Assert.NotNull(rabbitOptions);
        Assert.Equal("ThisIsATest", rabbitOptions.Host);
        Assert.Equal(1234, rabbitOptions.Port);
        Assert.Equal("TestUser", rabbitOptions.Username);
        Assert.Equal("TestPassword", rabbitOptions.Password);
    }

    [Fact]
    public void WebAppHostShouldSendCommandLineArgs()
    {
        WebApplicationBuilder builder = RabbitMQHost.CreateWebApplicationBuilder(new[]
        {
            "RabbitHostCommandKey=RabbitHostCommandValue"
        });

        using WebApplication webApp = builder.Build();
        webApp.Start();
        var config = webApp.Services.GetService<IConfiguration>();

        Assert.Equal("RabbitHostCommandValue", config["RabbitHostCommandKey"]);
        webApp.StopAsync();
    }

    [Fact]
    public void WebAppShouldWorkWithRabbitMQConnection()
    {
        WebApplicationBuilder builder = RabbitMQHost.CreateWebApplicationBuilder();
        builder.Services.AddRabbitMQConnection(new ConfigurationBuilder().Build());
        using WebApplication webApp = builder.Build();
        var connectionFactory = webApp.Services.GetRequiredService<RC.IConnectionFactory>();

        Assert.NotNull(connectionFactory);
    }
}