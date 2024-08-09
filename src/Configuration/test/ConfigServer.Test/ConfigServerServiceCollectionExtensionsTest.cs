// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerServiceCollectionExtensionsTest
{
    [Fact]
    public void ConfigureConfigServerClientOptions_ConfiguresConfigServerClientOptions_WithDefaults()
    {
        var services = new ServiceCollection();
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment("Production");

        IConfigurationBuilder builder = new ConfigurationBuilder().AddConfigServer(environment);
        IConfigurationRoot configurationRoot = builder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.ConfigureConfigServerClientOptions();
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var service = serviceProvider.GetRequiredService<IOptions<ConfigServerClientOptions>>();

        TestHelper.VerifyDefaults(service.Value);
    }

    [Fact]
    public void ConfigureConfigServerClientOptions_ConfiguresCloudFoundryOptions()
    {
        var services = new ServiceCollection();

        services.ConfigureConfigServerClientOptions();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var app = serviceProvider.GetRequiredService<IOptions<CloudFoundryApplicationOptions>>();
        Assert.NotNull(app.Value.ApplicationName);

        var service = serviceProvider.GetRequiredService<IOptions<CloudFoundryServicesOptions>>();
        Assert.Empty(service.Value.Services);
    }
}
