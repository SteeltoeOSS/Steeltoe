// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class ServiceCollectionExtensionsTest
{
    [Fact]
    public async Task Registers_dependent_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<IServer, TestServer>();
        services.AddSpringBootAdminClient();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<IHostedService>().OfType<SpringBootAdminClientHostedService>().Should().ContainSingle();
    }
}
