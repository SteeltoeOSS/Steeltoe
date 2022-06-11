// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Extensions.Configuration.CloudFoundry.Test;

public class CloudFoundryConfigurationProviderTest
{
    [Fact]
    public void Constructor_NullReader()
    {
        Assert.Throws<ArgumentNullException>(() => new CloudFoundryConfigurationProvider(null));
    }

    [Fact]
    public void Load_VCAP_APPLICATION_ChangesDataDictionary()
    {
        var environment = @"
                {
                    ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
                    ""application_name"": ""my-app"",
                    ""application_uris"": [ ""my-app.10.244.0.34.xip.io""],
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
                }";

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", environment);
        var provider = new CloudFoundryConfigurationProvider(new CloudFoundryEnvironmentSettingsReader());

        provider.Load();
        var dict = provider.Properties;
        Assert.Equal("fa05c1a9-0fc1-4fbd-bae1-139850dec7a3", dict["vcap:application:application_id"]);
        Assert.Equal("1024", dict["vcap:application:limits:disk"]);
        Assert.Equal("my-app.10.244.0.34.xip.io", dict["vcap:application:uris:0"]);
        Assert.Equal("my-app2.10.244.0.34.xip.io", dict["vcap:application:uris:1"]);
    }

    [Fact]
    public void Load_VCAP_SERVICES_ChangesDataDictionary()
    {
        var environment = @"
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
                    ""sendgrid"": [{
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
                }";

        Environment.SetEnvironmentVariable("VCAP_SERVICES", environment);
        var provider = new CloudFoundryConfigurationProvider(new CloudFoundryEnvironmentSettingsReader());

        provider.Load();
        var dict = provider.Properties;
        Assert.Equal("elephantsql-c6c60", dict["vcap:services:elephantsql:0:name"]);
        Assert.Equal("mysendgrid", dict["vcap:services:sendgrid:0:name"]);
    }

    [Fact]
    public void Load_VCAP_SERVICES_MultiServices_ChangesDataDictionary()
    {
        var environment = @"
                {
                    ""p-config-server"": [{
                        ""name"": ""myConfigServer"",
                        ""label"": ""p-config-server"",
                        ""tags"": [""configuration"",""spring-cloud""],
                        ""plan"": ""standard"",
                        ""credentials"": {
                            ""uri"": ""https://config-eafc353b-77e2-4dcc-b52a-25777e996ed9.apps.testcloud.com"",
                            ""client_id"": ""p-config-server-9bff4c87-7ffd-4536-9e76-e67ea3ec81d0"",
                            ""client_secret"": ""AJUAjyxP3nO9"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        }
                    }],
                    ""p-service-registry"": [{
                        ""name"": ""myServiceRegistry"",
                        ""label"": ""p-service-registry"",
                        ""tags"": [
                            ""eureka"",
                            ""discovery"",
                            ""registry"",
                            ""spring-cloud""
                        ],
                        ""plan"": ""standard"",
                        ""credentials"": {
                            ""uri"": ""https://eureka-f4b98d1c-3166-4741-b691-79abba5b2d51.apps.testcloud.com"",
                            ""client_id"": ""p-service-registry-9121b185-cd3b-497c-99f7-8e8064d4a6f0"",
                            ""client_secret"": ""3Rv1U79siLDa"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        }
                    }],
                    ""p-mysql"": [{
                        ""name"": ""mySql1"",
                        ""label"": ""p-mysql"",
                        ""tags"": [""mysql"",""relational""],
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
                        ""tags"": [""mysql"",""relational""],
                        ""plan"": ""100mb-dev"",
                        ""credentials"": {
                            ""hostname"": ""192.168.0.97"",
                            ""port"": 3306,
                            ""name"": ""cf_b2d83697_5fa1_4a51_991b_975c9d7e5515"",
                            ""username"": ""gxXQb2pMbzFsZQW8"",
                            ""password"": ""lvMkGf6oJQvKSOwn"",
                            ""uri"": ""mysql://gxXQb2pMbzFsZQW8:lvMkGf6oJQvKSOwn@192.168.0.97:3306/cf_b2d83697_5fa1_4a51_991b_975c9d7e5515?reconnect=true"",
                            ""jdbcUrl"": ""jdbc:mysql://192.168.0.97:3306/cf_b2d83697_5fa1_4a51_991b_975c9d7e5515?user=gxXQb2pMbzFsZQW8&password=lvMkGf6oJQvKSOwn""
                        }
                    }]
                }";

        Environment.SetEnvironmentVariable("VCAP_SERVICES", environment);
        var provider = new CloudFoundryConfigurationProvider(new CloudFoundryEnvironmentSettingsReader());

        provider.Load();
        var dict = provider.Properties;
        Assert.Equal("myConfigServer", dict["vcap:services:p-config-server:0:name"]);
        Assert.Equal("https://config-eafc353b-77e2-4dcc-b52a-25777e996ed9.apps.testcloud.com", dict["vcap:services:p-config-server:0:credentials:uri"]);
        Assert.Equal("myServiceRegistry", dict["vcap:services:p-service-registry:0:name"]);
        Assert.Equal("https://eureka-f4b98d1c-3166-4741-b691-79abba5b2d51.apps.testcloud.com", dict["vcap:services:p-service-registry:0:credentials:uri"]);
        Assert.Equal("mySql1", dict["vcap:services:p-mysql:0:name"]);
        Assert.Equal("mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true", dict["vcap:services:p-mysql:0:credentials:uri"]);
        Assert.Equal("mySql2", dict["vcap:services:p-mysql:1:name"]);
        Assert.Equal("mysql://gxXQb2pMbzFsZQW8:lvMkGf6oJQvKSOwn@192.168.0.97:3306/cf_b2d83697_5fa1_4a51_991b_975c9d7e5515?reconnect=true", dict["vcap:services:p-mysql:1:credentials:uri"]);
    }

    [Fact]
    public void Load_VCAP_APPLICATION_Allows_Reload_Without_Throwing_Exception()
    {
        var environment = @"
                {
                    ""name"": ""my-app"",
                    ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
                }";

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", environment);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddCloudFoundry();

        var configuration = configurationBuilder.Build();

        VcapApp options = null;

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        void ReloadLoop()
        {
            while (!cts.IsCancellationRequested)
            {
                configuration.Reload();
            }
        }

        _ = Task.Run(ReloadLoop);

        while (!cts.IsCancellationRequested)
        {
            options = configuration.GetSection("vcap:application").Get<VcapApp>();
        }

        Assert.Equal("my-app", options.Name);
        Assert.Equal("fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca", options.Version);
    }

    private sealed class VcapApp
    {
#pragma warning disable S3459 // Unassigned members should be removed
        public string Name { get; set; }

        public string Version { get; set; }
#pragma warning restore S3459 // Unassigned members should be removed
    }
}
