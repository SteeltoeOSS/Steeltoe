// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.SpringBoot;
using Steeltoe.Stream.Extensions;
using Steeltoe.Stream.StreamHost;
using Steeltoe.Stream.Test.StreamsHost;
using Xunit;

namespace Steeltoe.Stream.Test.Extensions;

public sealed class HostBuilderExtensionsTest
{
    [Fact]
    public void HostBuilderExtensionTest()
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder().UseDefaultServiceProvider(options => options.ValidateScopes = true)
            .AddStreamServices<SampleSink>();

        hostBuilder.ConfigureServices(services => services.AddSingleton(isp => isp.GetRequiredService<IConfiguration>() as IConfigurationRoot));
        IHost host = hostBuilder.Build();
        var configurationRoot = host.Services.GetService<IConfigurationRoot>();
        Assert.NotNull(hostBuilder);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(svc => svc is StreamLifeCycleService));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootEnvironmentVariableProvider));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootCommandLineProvider));
    }

    [Fact]
    public void WebHostBuilderExtensionTest()
    {
        IWebHostBuilder hostBuilder = WebHost.CreateDefaultBuilder().UseDefaultServiceProvider(options => options.ValidateScopes = true)
            .Configure(HostingHelpers.EmptyAction).AddStreamServices<SampleSink>();

        hostBuilder.ConfigureServices(services => services.AddSingleton(isp => isp.GetRequiredService<IConfiguration>() as IConfigurationRoot));
        IWebHost host = hostBuilder.Build();
        var configurationRoot = host.Services.GetService<IConfigurationRoot>();
        Assert.NotNull(hostBuilder);
        Assert.Single(host.Services.GetServices<IHostedService>().Where(svc => svc is StreamLifeCycleService));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootEnvironmentVariableProvider));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootCommandLineProvider));
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
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootEnvironmentVariableProvider));
        Assert.Single(configurationRoot.Providers.Where(p => p is SpringBootCommandLineProvider));
    }
}
