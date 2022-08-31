// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test;

public class ConfigServerServiceCollectionExtensionsTest
{
    [Fact]
    public void ConfigureConfigServerClientOptions_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        var ex = Assert.Throws<ArgumentNullException>(() => services.ConfigureConfigServerClientOptions());
        Assert.Contains(nameof(services), ex.Message);
    }

    [Fact]
    public void ConfigureConfigServerClientOptions_ConfiguresConfigServerClientSettingsOptions_WithDefaults()
    {
        var services = new ServiceCollection();
        IHostEnvironment environment = HostingHelpers.GetHostingEnvironment("Production");

        IConfigurationBuilder builder = new ConfigurationBuilder().AddConfigServer(environment.EnvironmentName);
        IConfigurationRoot configurationRoot = builder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.ConfigureConfigServerClientOptions();
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetService<IOptions<ConfigServerClientSettingsOptions>>();
        Assert.NotNull(service);
        ConfigServerClientSettingsOptions options = service.Value;
        Assert.NotNull(options);
        TestHelper.VerifyDefaults(options.Settings);
    }

    [Fact]
    public void ConfigureConfigServerClientOptions_ConfiguresCloudFoundryOptions()
    {
        var services = new ServiceCollection();

        services.ConfigureConfigServerClientOptions();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var app = serviceProvider.GetService<IOptions<CloudFoundryApplicationOptions>>();
        Assert.NotNull(app);
        var service = serviceProvider.GetService<IOptions<CloudFoundryServicesOptions>>();
        Assert.NotNull(service);
    }
}
