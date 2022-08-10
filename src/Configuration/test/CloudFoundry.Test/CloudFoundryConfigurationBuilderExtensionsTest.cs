// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test;

public class CloudFoundryConfigurationBuilderExtensionsTest
{
    [Fact]
    public void AddCloudFoundry_ThrowsIfConfigBuilderNull()
    {
        const IConfigurationBuilder configurationBuilder = null;

        var ex = Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddCloudFoundry());
        Assert.Contains(nameof(configurationBuilder), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddCloudFoundry(null));
        Assert.Contains(nameof(configurationBuilder), ex2.Message);
    }

    [Fact]
    public void AddCloudFoundry_AddsCloudFoundrySourceToSourcesList()
    {
        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddCloudFoundry();

        CloudFoundryConfigurationSource cloudSource = null;

        foreach (IConfigurationSource source in configurationBuilder.Sources)
        {
            cloudSource = source as CloudFoundryConfigurationSource;

            if (cloudSource != null)
            {
                break;
            }
        }

        Assert.NotNull(cloudSource);
    }

    [Fact]
    public void AddCloudFoundry_CanReadSettingsFromMemory()
    {
        var reader = new CloudFoundryMemorySettingsReader
        {
            ApplicationJson = @"
                    {
                        ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
                        ""application_name"": ""my-app"",
                        ""application_uris"": [""my-app.10.244.0.34.xip.io""],
                        ""application_version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"",
                        ""limits"": {
                            ""disk"": 1024,
                            ""fds"": 16384,
                            ""mem"": 256
                        },
                        ""name"": ""my-app"",
                        ""space_id"": ""06450c72-4669-4dc6-8096-45f9777db68a"",
                        ""space_name"": ""my-space"",
                        ""uris"": [
                            ""my-app.10.244.0.34.xip.io"",
                            ""my-app2.10.244.0.34.xip.io""
                        ],
                        ""users"": null,
                        ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                    }",
            ServicesJson = @"
                    {
                        ""elephantsql"": [{
                            ""name"": ""elephantsql-c6c60"",
                            ""label"": ""elephantsql"",
                            ""tags"": [
                                ""postgres"",
                                ""postgresql"",
                                ""relational""
                            ],
                            ""plan"": ""turtle"",
                            ""credentials"": {""uri"": ""postgres://seilbmbd:ABcdEF@babar.elephantsql.com:5432/seilbmbd""}
                        }],
                        ""sendgrid"": [
                        {
                            ""name"": ""mysendgrid"",
                            ""label"": ""sendgrid"",
                            ""tags"": [""smtp""],
                            ""plan"": ""free"",
                            ""credentials"": {
                                ""hostname"": ""smtp.sendgrid.net"",
                                ""username"": ""QvsXMbJ3rK"",
                                ""password"": ""HCHMOYluTv""
                            }
                        }]
                    }",
            InstanceId = "7c19d892-21c2-496b-a42a-946bbaa0775e",
            InstanceIndex = "0",
            InstanceInternalIp = "127.0.0.1",
            InstanceIp = "10.41.1.1",
            InstancePort = "8888"
        };

        IConfigurationRoot configuration = new ConfigurationBuilder().AddCloudFoundry(reader).Build();

        Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", configuration["vcap:application:application_id"]);
        Assert.Equal("1024", configuration["vcap:application:limits:disk"]);
        Assert.Equal("my-app.10.244.0.34.xip.io", configuration["vcap:application:uris:0"]);
        Assert.Equal("my-app2.10.244.0.34.xip.io", configuration["vcap:application:uris:1"]);
        Assert.Equal("elephantsql-c6c60", configuration["vcap:services:elephantsql:0:name"]);
        Assert.Equal("mysendgrid", configuration["vcap:services:sendgrid:0:name"]);

        Assert.Equal("7c19d892-21c2-496b-a42a-946bbaa0775e", configuration["vcap:application:instance_id"]);
        Assert.Equal("0", configuration["vcap:application:instance_index"]);
        Assert.Equal("127.0.0.1", configuration["vcap:application:internal_ip"]);
        Assert.Equal("10.41.1.1", configuration["vcap:application:instance_ip"]);
        Assert.Equal("8888", configuration["vcap:application:port"]);
    }
}
