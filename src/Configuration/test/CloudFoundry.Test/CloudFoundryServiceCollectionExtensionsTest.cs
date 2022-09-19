// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test;

public sealed class CloudFoundryServiceCollectionExtensionsTest
{
    [Fact]
    public void ConfigureCloudFoundryOptions_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryOptions(configurationRoot));
        Assert.Contains(nameof(services), ex.Message);
    }

    [Fact]
    public void ConfigureCloudFoundryOptions_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryOptions(configuration));
        Assert.Contains(nameof(configuration), ex.Message);
    }

    [Fact]
    public void ConfigureCloudFoundryOptions_ConfiguresCloudFoundryOptions()
    {
        var services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION",
            @"{ ""cf_api"": ""https://api.run.pcfone.io"", ""limits"": { ""fds"": 16384 }, ""application_name"": ""foo"", ""application_uris"": [ ""foo-unexpected-serval-iy.apps.pcfone.io"" ], ""name"": ""foo"", ""space_name"": ""playground"", ""space_id"": ""f03f2ab0-cf33-416b-999c-fb01c1247753"", ""organization_id"": ""d7afe5cb-2d42-487b-a415-f47c0665f1ba"", ""organization_name"": ""pivot-thess"", ""uris"": [ ""foo-unexpected-serval-iy.apps.pcfone.io"" ], ""users"": null, ""application_id"": ""f69a6624-7669-43e3-a3c8-34d23a17e3db"" }");

        IConfigurationBuilder builder = new ConfigurationBuilder().AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();
        services.ConfigureCloudFoundryOptions(configurationRoot);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var app = serviceProvider.GetRequiredService<IOptions<CloudFoundryApplicationOptions>>();
        Assert.NotNull(app.Value);
        Assert.Equal("foo", app.Value.ApplicationName);
        Assert.Equal("playground", app.Value.SpaceName);
        var service = serviceProvider.GetRequiredService<IOptions<CloudFoundryServicesOptions>>();
        Assert.NotNull(service.Value);
    }

    [Fact]
    public void ConfigureCloudFoundryService_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryService<MySqlServicesOptions>(configurationRoot, "foobar"));
    }

    [Fact]
    public void ConfigureCloudFoundryService_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;

        Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryService<MySqlServicesOptions>(configurationRoot, "foobar"));
    }

    [Fact]
    public void ConfigureCloudFoundryService_BadServiceName()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryService<MySqlServicesOptions>(configurationRoot, null));
        Assert.Throws<ArgumentException>(() => services.ConfigureCloudFoundryService<MySqlServicesOptions>(configurationRoot, string.Empty));
    }

    [Fact]
    public void ConfigureCloudFoundryService_ConfiguresService()
    {
        const string configJson = @"
                {
                  ""vcap"": {
                    ""services"" : {
                            ""p-mysql"": [{
                                ""name"": ""mySql1"",
                                ""label"": ""p-mysql"",
                                ""tags"": [
                                    ""mysql"",
                                    ""relational""
                                ],
                                ""plan"": ""100mb-dev"",
                                ""credentials"": {
                                    ""hostname"": ""192.168.0.97"",
                                    ""port"": 3306,
                                    ""name"": ""cf_0f5dda44_e678_4727_993f_30e6d455cc31"",
                                    ""username"": ""9vD0Mtk3wFFuaaaY"",
                                    ""password"": ""Cjn4HsAiKV8sImst"",
                                    ""uri"": ""mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true"",
                                    ""jdbcUrl"": ""jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst""
                                }
                            },
                            {
                                ""name"": ""mySql2"",
                                ""label"": ""p-mysql"",
                                ""tags"": [
                                    ""mysql"",
                                    ""relational""
                                ],
                                ""plan"": ""100mb-dev"",
                                ""credentials"": {
                                    ""hostname"": ""192.168.0.97"",
                                    ""port"": 3306,
                                    ""name"": ""cf_0f5dda44_e678_4727_993f_30e6d455cc31"",
                                    ""username"": ""9vD0Mtk3wFFuaaaY"",
                                    ""password"": ""Cjn4HsAiKV8sImst"",
                                    ""uri"": ""mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true"",
                                    ""jdbcUrl"": ""jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst""
                                }
                            }]
                        }
                    }
                }";

        using Stream stream = CloudFoundryConfigurationProvider.GetStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(stream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot configurationRoot = builder.Build();
        var services = new ServiceCollection();
        services.AddOptions();

        services.ConfigureCloudFoundryService<MySqlServicesOptions>(configurationRoot, "mySql2");

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var snapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<MySqlServicesOptions>>();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<MySqlServicesOptions>>();
        MySqlServicesOptions optionsInSnapshot = snapshot.Get("mySql2");
        MySqlServicesOptions optionsInMonitor = monitor.Get("mySql2");
        Assert.NotNull(optionsInSnapshot);
        Assert.NotNull(optionsInMonitor);

        Assert.Equal("mySql2", optionsInSnapshot.Name);
        Assert.Equal("p-mysql", optionsInSnapshot.Label);
        Assert.Equal("mySql2", optionsInMonitor.Name);
        Assert.Equal("p-mysql", optionsInMonitor.Label);
    }

    [Fact]
    public void ConfigureCloudFoundryServices_ConfiguresServices()
    {
        const string configJson = @"
                {
                    ""vcap"": {
                        ""services"" : {
                            ""p-mysql"": [{
                                ""name"": ""mySql1"",
                                ""label"": ""p-mysql"",
                                ""tags"": [
                                    ""mysql"",
                                    ""relational""
                                ],
                                ""plan"": ""100mb-dev"",
                                ""credentials"": {
                                    ""hostname"": ""192.168.0.97"",
                                    ""port"": 3306,
                                    ""name"": ""cf_0f5dda44_e678_4727_993f_30e6d455cc31"",
                                    ""username"": ""9vD0Mtk3wFFuaaaY"",
                                    ""password"": ""Cjn4HsAiKV8sImst"",
                                    ""uri"": ""mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true"",
                                    ""jdbcUrl"": ""jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst""
                                }
                            },
                            {
                                ""name"": ""mySql2"",
                                ""label"": ""p-mysql"",
                                ""tags"": [
                                    ""mysql"",
                                    ""relational""
                                ],
                                ""plan"": ""100mb-dev"",
                                ""credentials"": {
                                    ""hostname"": ""192.168.0.97"",
                                    ""port"": 3306,
                                    ""name"": ""cf_0f5dda44_e678_4727_993f_30e6d455cc31"",
                                    ""username"": ""9vD0Mtk3wFFuaaaY"",
                                    ""password"": ""Cjn4HsAiKV8sImst"",
                                    ""uri"": ""mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true"",
                                    ""jdbcUrl"": ""jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst""
                                }
                            }]
                        }
                    }
                }";

        using Stream stream = CloudFoundryConfigurationProvider.GetStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(stream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot configurationRoot = builder.Build();
        var services = new ServiceCollection();
        services.AddOptions();

        services.ConfigureCloudFoundryServices<MySqlServicesOptions>(configurationRoot, "p-mysql");

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var snapshot = serviceProvider.GetRequiredService<IOptionsSnapshot<MySqlServicesOptions>>();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<MySqlServicesOptions>>();

        MySqlServicesOptions optionsInSnapshot1 = snapshot.Get("mySql1");
        MySqlServicesOptions optionsInMonitor1 = monitor.Get("mySql1");
        Assert.NotNull(optionsInSnapshot1);
        Assert.NotNull(optionsInMonitor1);

        Assert.Equal("mySql1", optionsInSnapshot1.Name);
        Assert.Equal("p-mysql", optionsInSnapshot1.Label);
        Assert.Equal("mySql1", optionsInMonitor1.Name);
        Assert.Equal("p-mysql", optionsInMonitor1.Label);

        MySqlServicesOptions optionsInSnapshot2 = snapshot.Get("mySql2");
        MySqlServicesOptions optionsInMonitor2 = monitor.Get("mySql2");
        Assert.NotNull(optionsInSnapshot2);
        Assert.NotNull(optionsInMonitor2);

        Assert.Equal("mySql2", optionsInSnapshot2.Name);
        Assert.Equal("p-mysql", optionsInSnapshot2.Label);
        Assert.Equal("mySql2", optionsInMonitor2.Name);
        Assert.Equal("p-mysql", optionsInMonitor2.Label);
    }
}
