// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Net;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class ServiceCollectionExtensionsTest
{
    [Fact]
    public async Task AddSpringBootAdminClient_AddsHostedService()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton<IServer, TestServer>();
        services.AddLogging();
        services.AddSpringBootAdminClient();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetService<IApplicationInstanceInfo>().Should().NotBeNull();
        serviceProvider.GetService<InetUtils>().Should().NotBeNull();
        serviceProvider.GetService<TimeProvider>().Should().NotBeNull();
        serviceProvider.GetService<AppUrlCalculator>().Should().NotBeNull();
        serviceProvider.GetService<SpringBootAdminRefreshRunner>().Should().NotBeNull();
        serviceProvider.GetServices<IHostedService>().OfType<SpringBootAdminClientHostedService>().Should().ContainSingle();
    }
}
