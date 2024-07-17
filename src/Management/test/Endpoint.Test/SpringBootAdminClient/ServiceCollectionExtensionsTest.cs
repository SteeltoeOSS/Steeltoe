// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddSpringBootAdminClient_AddsHostedService()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().AddCommandLine(new[]
        {
            "--urls=http://localhost"
        }).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddSpringBootAdminClient();
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        Assert.NotEmpty(serviceProvider.GetServices<IHostedService>().OfType<SpringBootAdminClientHostedService>());
    }
}
