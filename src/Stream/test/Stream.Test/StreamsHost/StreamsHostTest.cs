// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Xunit;

namespace Steeltoe.Stream.StreamHost;

public class StreamsHostTest
{
    [Fact]
    public void HostCanBeStarted()
    {
        FakeHostedService service;

        using (IHost host = StreamHost.CreateDefaultBuilder<SampleSink>().ConfigureServices(svc => svc.AddSingleton<IHostedService, FakeHostedService>())
            .Start())
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
    [Trait("Category", "SkipOnMacOS")]
    [Trait("Category", "SkipOnLinux")]
    public void HostConfiguresRabbitOptions()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", GetCloudFoundryRabbitMqConfiguration());
        using IHost host = StreamHost.CreateDefaultBuilder<SampleSink>().ConfigureAppConfiguration(c => c.AddCloudFoundry()).Start();
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
                    ""uri"": ""amqp://Dd6O1BPXUHdrmzbP:7E1LxXnlH2hhlPVt@192.168.0.90:3306/cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355""
                },
                ""syslog_drain_url"": null,
                ""label"": ""p-rabbitmq"",
                ""provider"": null,
                ""plan"": ""standard"",
                ""name"": ""myRabbitMQService1"",
                ""tags"": [
                    ""rabbitmq"",
                    ""amqp""
                ]
            }]
        }";
    }
}
