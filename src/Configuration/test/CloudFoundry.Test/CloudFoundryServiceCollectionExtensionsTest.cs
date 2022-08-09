// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test;

public class CloudFoundryServiceCollectionExtensionsTest
{
    [Fact]
    public void ConfigureCloudFoundryOptions_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryOptions(config));
        Assert.Contains(nameof(services), ex.Message);
    }

    [Fact]
    public void ConfigureCloudFoundryOptions_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryOptions(config));
        Assert.Contains(nameof(config), ex.Message);
    }

    [Fact]
    public void ConfigureCloudFoundryOptions_ConfiguresCloudFoundryOptions()
    {
        var services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION",
            @"{ ""cf_api"": ""https://api.run.pcfone.io"", ""limits"": { ""fds"": 16384 }, ""application_name"": ""foo"", ""application_uris"": [ ""foo-unexpected-serval-iy.apps.pcfone.io"" ], ""name"": ""foo"", ""space_name"": ""playground"", ""space_id"": ""f03f2ab0-cf33-416b-999c-fb01c1247753"", ""organization_id"": ""d7afe5cb-2d42-487b-a415-f47c0665f1ba"", ""organization_name"": ""pivot-thess"", ""uris"": [ ""foo-unexpected-serval-iy.apps.pcfone.io"" ], ""users"": null, ""application_id"": ""f69a6624-7669-43e3-a3c8-34d23a17e3db"" }");

        IConfigurationBuilder builder = new ConfigurationBuilder().AddCloudFoundry();
        IConfigurationRoot config = builder.Build();
        services.ConfigureCloudFoundryOptions(config);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var app = serviceProvider.GetService<IOptions<CloudFoundryApplicationOptions>>();
        Assert.NotNull(app.Value);
        Assert.Equal("foo", app.Value.ApplicationName);
        Assert.Equal("playground", app.Value.SpaceName);
        var service = serviceProvider.GetService<IOptions<CloudFoundryServicesOptions>>();
        Assert.NotNull(service.Value);
    }

    [Fact]
    public void ConfigureCloudFoundryService_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot config = null;

        Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryService<MySqlServiceOption>(config, "foobar"));
    }

    [Fact]
    public void ConfigureCloudFoundryService_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;

        Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryService<MySqlServiceOption>(config, "foobar"));
    }

    [Fact]
    public void ConfigureCloudFoundryService_BadServiceName()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() => services.ConfigureCloudFoundryService<MySqlServiceOption>(config, null));
        Assert.Throws<ArgumentException>(() => services.ConfigureCloudFoundryService<MySqlServiceOption>(config, string.Empty));
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

        MemoryStream memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(memStream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot config = builder.Build();
        var services = new ServiceCollection();
        services.AddOptions();

        services.ConfigureCloudFoundryService<MySqlServiceOption>(config, "mySql2");

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var snapShot = serviceProvider.GetRequiredService<IOptionsSnapshot<MySqlServiceOption>>();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<MySqlServiceOption>>();
        MySqlServiceOption snapOpt = snapShot.Get("mySql2");
        MySqlServiceOption monOpt = monitor.Get("mySql2");
        Assert.NotNull(snapOpt);
        Assert.NotNull(monOpt);

        Assert.Equal("mySql2", snapOpt.Name);
        Assert.Equal("p-mysql", snapOpt.Label);
        Assert.Equal("mySql2", monOpt.Name);
        Assert.Equal("p-mysql", monOpt.Label);
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

        MemoryStream memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(memStream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot config = builder.Build();
        var services = new ServiceCollection();
        services.AddOptions();

        services.ConfigureCloudFoundryServices<MySqlServiceOption>(config, "p-mysql");

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var snapShot = serviceProvider.GetRequiredService<IOptionsSnapshot<MySqlServiceOption>>();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<MySqlServiceOption>>();

        MySqlServiceOption snapOpt1 = snapShot.Get("mySql1");
        MySqlServiceOption monOpt1 = monitor.Get("mySql1");
        Assert.NotNull(snapOpt1);
        Assert.NotNull(monOpt1);

        Assert.Equal("mySql1", snapOpt1.Name);
        Assert.Equal("p-mysql", snapOpt1.Label);
        Assert.Equal("mySql1", monOpt1.Name);
        Assert.Equal("p-mysql", monOpt1.Label);

        MySqlServiceOption snapOpt2 = snapShot.Get("mySql2");
        MySqlServiceOption monOpt2 = monitor.Get("mySql2");
        Assert.NotNull(snapOpt2);
        Assert.NotNull(monOpt2);

        Assert.Equal("mySql2", snapOpt2.Name);
        Assert.Equal("p-mysql", snapOpt2.Label);
        Assert.Equal("mySql2", monOpt2.Name);
        Assert.Equal("p-mysql", monOpt2.Label);
    }
}
