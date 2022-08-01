// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Configuration.SpringBoot;
using Steeltoe.Stream.StreamHost;
using Xunit;

namespace Steeltoe.Stream.Extensions;

public partial class HostBuilderExtensionsTest
{
    [Fact]
    public void HostBuilderExtensionTest()
    {
        var hostBuilder = Host.CreateDefaultBuilder().AddStreamServices<SampleSink>();
        hostBuilder.ConfigureServices(services => services.AddSingleton(isp => isp.GetRequiredService<IConfiguration>() as IConfigurationRoot));
        var host = hostBuilder.Build();
        var configRoot = host.Services.GetService<IConfigurationRoot>();
        Assert.NotNull(hostBuilder);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(svc => svc is StreamLifeCycleService));
        Assert.Single(configRoot.Providers.Where(p => p is SpringBootEnvProvider));
        Assert.Single(configRoot.Providers.Where(p => p is SpringBootCmdProvider));
    }

    [Fact]
    public void WebHostBuilderExtensionTest()
    {
        var hostBuilder = WebHost.CreateDefaultBuilder().Configure(_ => { }).AddStreamServices<SampleSink>();
        hostBuilder.ConfigureServices(services => services.AddSingleton(isp => isp.GetRequiredService<IConfiguration>() as IConfigurationRoot));
        var host = hostBuilder.Build();
        var configRoot = host.Services.GetService<IConfigurationRoot>();
        Assert.NotNull(hostBuilder);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(svc => svc is StreamLifeCycleService));
        Assert.Single(configRoot.Providers.Where(p => p is SpringBootEnvProvider));
        Assert.Single(configRoot.Providers.Where(p => p is SpringBootCmdProvider));
    }

    [Fact]
    public void WebApplicationBuilderExtensionTest()
    {
        var hostBuilder = TestHelpers.GetTestWebApplicationBuilder().AddStreamServices<SampleSink>();
        hostBuilder.Services.AddSingleton(isp => isp.GetRequiredService<IConfiguration>() as IConfigurationRoot);
        var host = hostBuilder.Build();
        var configRoot = host.Services.GetService<IConfigurationRoot>();
        Assert.NotNull(hostBuilder);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(svc => svc is StreamLifeCycleService));
        Assert.Single(configRoot.Providers.Where(p => p is SpringBootEnvProvider));
        Assert.Single(configRoot.Providers.Where(p => p is SpringBootCmdProvider));
    }
}
