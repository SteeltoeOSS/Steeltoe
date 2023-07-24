// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class ServiceCollectionExtensionsTest
{
    [Fact]
    public void AddSpringBootAdminClient_ThrowsOnNull()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddSpringBootAdminClient(null));
        Assert.Contains("services", ex.Message, StringComparison.Ordinal);
    }

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
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        Assert.IsType<SpringBootAdminClientHostedService>(serviceProvider.GetRequiredService<IHostedService>());
    }
}
