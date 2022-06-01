// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
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
    [Obsolete]
    public void ConfigureConfigServerClientOptions_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.ConfigureConfigServerClientOptions(config));
        Assert.Contains(nameof(config), ex.Message);
    }

    [Fact]
    public void ConfigureConfigServerClientOptions_ConfiguresConfigServerClientSettingsOptions_WithDefaults()
    {
        var services = new ServiceCollection();
        var environment = HostingHelpers.GetHostingEnvironment("Production");

        var builder = new ConfigurationBuilder().AddConfigServer(environment.EnvironmentName);
        var config = builder.Build();
        services.AddSingleton<IConfiguration>(config);
        services.ConfigureConfigServerClientOptions();
        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetService<IOptions<ConfigServerClientSettingsOptions>>();
        Assert.NotNull(service);
        var options = service.Value;
        Assert.NotNull(options);
        TestHelper.VerifyDefaults(options.Settings);
    }

    [Fact]
    public void ConfigureConfigServerClientOptions_ConfiguresCloudFoundryOptions()
    {
        var services = new ServiceCollection();
        var environment = HostingHelpers.GetHostingEnvironment();

        var builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { "spring:cloud:config:timeout", "10" } }).AddConfigServer(environment.EnvironmentName);
        var config = builder.Build();
        services.ConfigureConfigServerClientOptions();

        var serviceProvider = services.BuildServiceProvider();
        var app = serviceProvider.GetService<IOptions<CloudFoundryApplicationOptions>>();
        Assert.NotNull(app);
        var service = serviceProvider.GetService<IOptions<CloudFoundryServicesOptions>>();
        Assert.NotNull(service);
    }
}
