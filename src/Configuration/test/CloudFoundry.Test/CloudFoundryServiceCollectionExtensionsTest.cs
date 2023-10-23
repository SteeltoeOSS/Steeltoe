// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Configuration.CloudFoundry.Test;

public sealed class CloudFoundryServiceCollectionExtensionsTest
{
    [Fact]
    public void ConfigureCloudFoundryOptions_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryOptions(configurationRoot));
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureCloudFoundryOptions_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryOptions(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureCloudFoundryOptions_ConfiguresCloudFoundryOptions()
    {
        using var scope = new EnvironmentVariableScope("VCAP_APPLICATION",
            @"{ ""cf_api"": ""https://api.run.pcfone.io"", ""limits"": { ""fds"": 16384 }, ""application_name"": ""foo"", ""application_uris"": [ ""foo-unexpected-serval-iy.apps.pcfone.io"" ], ""name"": ""foo"", ""space_name"": ""playground"", ""space_id"": ""f03f2ab0-cf33-416b-999c-fb01c1247753"", ""organization_id"": ""d7afe5cb-2d42-487b-a415-f47c0665f1ba"", ""organization_name"": ""pivot-thess"", ""uris"": [ ""foo-unexpected-serval-iy.apps.pcfone.io"" ], ""users"": null, ""application_id"": ""f69a6624-7669-43e3-a3c8-34d23a17e3db"" }");

        var services = new ServiceCollection();

        IConfigurationBuilder builder = new ConfigurationBuilder().AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();
        services.ConfigureCloudFoundryOptions(configurationRoot);

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var app = serviceProvider.GetRequiredService<IOptions<CloudFoundryApplicationOptions>>();
        Assert.NotNull(app.Value);
        Assert.Equal("foo", app.Value.ApplicationName);
        Assert.Equal("playground", app.Value.SpaceName);
        var service = serviceProvider.GetRequiredService<IOptions<CloudFoundryServicesOptions>>();
        Assert.NotNull(service.Value);
    }
}
