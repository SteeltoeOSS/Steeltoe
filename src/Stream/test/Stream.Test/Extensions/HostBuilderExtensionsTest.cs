// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Configuration.SpringBoot;
using Steeltoe.Stream.StreamHost;
using Xunit;

namespace Steeltoe.Stream.Extensions;

public class HostBuilderExtensionsTest
{
    [Fact]
    public void HostBuilderExtensionTest()
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder().AddStreamServices<SampleSink>();
        hostBuilder.ConfigureServices(services => services.AddSingleton(isp => isp.GetRequiredService<IConfiguration>() as IConfigurationRoot));
        IHost host = hostBuilder.Build();
        var configurationRoot = host.Services.GetService<IConfigurationRoot>();
        Assert.NotNull(hostBuilder);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(svc => svc is StreamLifeCycleService));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootEnvProvider));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootCmdProvider));
    }

    [Fact]
    public void WebHostBuilderExtensionTest()
    {
        IWebHostBuilder hostBuilder = WebHost.CreateDefaultBuilder().Configure(_ =>
        {
        }).AddStreamServices<SampleSink>();

        hostBuilder.ConfigureServices(services => services.AddSingleton(isp => isp.GetRequiredService<IConfiguration>() as IConfigurationRoot));
        IWebHost host = hostBuilder.Build();
        var configurationRoot = host.Services.GetService<IConfigurationRoot>();
        Assert.NotNull(hostBuilder);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(svc => svc is StreamLifeCycleService));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootEnvProvider));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootCmdProvider));
    }

    [Fact]
    public void WebApplicationBuilderExtensionTest()
    {
        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder().AddStreamServices<SampleSink>();
        hostBuilder.Services.AddSingleton(isp => isp.GetRequiredService<IConfiguration>() as IConfigurationRoot);
        WebApplication host = hostBuilder.Build();
        var configurationRoot = host.Services.GetService<IConfigurationRoot>();
        Assert.NotNull(hostBuilder);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(svc => svc is StreamLifeCycleService));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootEnvProvider));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootCmdProvider));
    }
}
