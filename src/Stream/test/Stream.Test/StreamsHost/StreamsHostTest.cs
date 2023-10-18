// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Connectors.RabbitMQ;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Xunit;

namespace Steeltoe.Stream.Test.StreamsHost;

public sealed class StreamsHostTest
{
    [Fact]
    public void HostCanBeStarted()
    {
        FakeHostedService service;

        using (IHost host = StreamHost.StreamHost.CreateDefaultBuilder<SampleSink>()
            .ConfigureServices(svc => svc.AddSingleton<IHostedService, FakeHostedService>()).Start())
        {
            Assert.NotNull(host);
            service = (FakeHostedService)host.Services.GetRequiredService<IHostedService>();
            Assert.NotNull(service);
            Assert.Equal(1, service.StartCount);
            Assert.Equal(0, service.StopCount);
            Assert.Equal(0, service.DisposeCount);
        }

        Assert.Equal(1, service.StartCount);
        Assert.Equal(0, service.StopCount);
        Assert.Equal(1, service.DisposeCount);
    }

    [Fact]
    public void HostConfiguresRabbitOptions()
    {
        IHostBuilder builder = StreamHost.StreamHost.CreateDefaultBuilder<SampleSink>();

        builder.ConfigureAppConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddCloudFoundryServiceBindings(new StringServiceBindingsReader(GetCloudFoundryRabbitMqConfiguration()));
            configurationBuilder.ConfigureRabbitMQ();
        });

        using IHost host = builder.Start();

        var rabbitOptionsMonitor = host.Services.GetService<IOptionsMonitor<RabbitOptions>>();
        Assert.NotNull(rabbitOptionsMonitor);
        RabbitOptions rabbitOptions = rabbitOptionsMonitor.CurrentValue;

        Assert.Equal("Dd6O1BPXUHdrmzbP", rabbitOptions.Username);
        Assert.Equal("7E1LxXnlH2hhlPVt", rabbitOptions.Password);
        Assert.Equal("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", rabbitOptions.VirtualHost);
        Assert.Equal("Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306", rabbitOptions.Addresses);
    }

    private static string GetCloudFoundryRabbitMqConfiguration()
    {
        return @"
        {
            ""p-rabbitmq"": [{
                ""credentials"": {
                    ""protocols"": {
                        ""amqp"": {
                            ""host"": ""192.168.0.90"",
                            ""password"": ""7E1LxXnlH2hhlPVt"",
                            ""port"": 3306,
                            ""username"": ""Dd6O1BPXUHdrmzbP"",
                            ""vhost"": ""cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355""
                      }
                  },
                  ""ssl"": false,
                },
                ""name"": ""myRabbitMQService1"",
                ""tags"": [
                    ""rabbitmq""
                ]
            }]
        }";
    }
}
