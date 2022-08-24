// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test;

public class AbstractServiceOptionsTest
{
    [Fact]
    public void Bind_ThrowsWithBadArguments()
    {
        var options = new MySqlServiceOption();
        Assert.Throws<ArgumentNullException>(() => options.Bind(null, "foobar"));
        Assert.Throws<ArgumentNullException>(() => options.Bind(new ConfigurationBuilder().Build(), null));
        Assert.Throws<ArgumentException>(() => options.Bind(new ConfigurationBuilder().Build(), string.Empty));
    }

    [Fact]
    public void Bind_BindsConfiguration()
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
        IConfigurationRoot root = builder.Build();

        var options1 = new MySqlServiceOption();
        options1.Bind(root, "mySql2");
        Assert.Equal("mySql2", options1.Name);
        Assert.Equal("p-mysql", options1.Label);

        var options2 = new MySqlServiceOption();
        options2.Bind(root, "mySql1");
        Assert.Equal("mySql1", options2.Name);
        Assert.Equal("p-mysql", options2.Label);
    }

    [Fact]
    public void Bind_DoesNotBindsConfiguration()
    {
        const string configJson = @"
            {
                ""foo"": {
                    ""bar"" : {
                    }
                }
            }";

        MemoryStream memStream = CloudFoundryConfigurationProvider.GetMemoryStream(configJson);
        var jsonSource = new JsonStreamConfigurationSource(memStream);
        IConfigurationBuilder builder = new ConfigurationBuilder().Add(jsonSource);
        IConfigurationRoot config = builder.Build();

        var opt = new MySqlServiceOption();
        opt.Bind(config, "mySql2");
        Assert.NotEqual("mySql2", opt.Name);
        Assert.NotEqual("p-mysql", opt.Label);
    }
}
