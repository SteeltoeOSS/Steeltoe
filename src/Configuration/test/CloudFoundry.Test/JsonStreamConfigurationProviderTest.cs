// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.Test;

public sealed class JsonStreamConfigurationProviderTest
{
    [Fact]
    public void Load_LoadsProvidedStream()
    {
        const string environment = """
            {
                "p-config-server": [{
                    "name": "myConfigServer",
                    "label": "p-config-server",
                    "tags": [
                        "configuration",
                        "spring-cloud"
                    ],
                    "plan": "standard",
                    "credentials": {
                        "uri": "https://config-eafc353b-77e2-4dcc-b52a-25777e996ed9.apps.test-cloud.com",
                        "client_id": "p-config-server-9bff4c87-7ffd-4536-9e76-e67ea3ec81d0",
                        "client_secret": "AJUAjyxP3nO9",
                        "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                    }
                }],
                "p-service-registry": [{
                    "name": "myServiceRegistry",
                    "label": "p-service-registry",
                    "tags": [
                        "eureka",
                        "discovery",
                        "registry",
                        "spring-cloud"
                    ],
                    "plan": "standard",
                    "credentials": {
                        "uri": "https://eureka-f4b98d1c-3166-4741-b691-79abba5b2d51.apps.test-cloud.com",
                        "client_id": "p-service-registry-9121b185-cd3b-497c-99f7-8e8064d4a6f0",
                        "client_secret": "3Rv1U79siLDa",
                        "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                    }
                }],
                "p-mysql": [{
                    "name": "mySql1",
                    "label": "p-mysql",
                    "tags": [
                        "mysql",
                        "relational"
                    ],
                    "plan": "100mb-dev",
                    "credentials": {
                        "hostname": "192.168.0.97",
                        "port": 3306,
                        "name": "cf_0f5dda44_e678_4727_993f_30e6d455cc31",
                        "username": "9vD0Mtk3wFFuaaaY",
                        "password": "Cjn4HsAiKV8sImst",
                        "uri": "mysql://9vD0Mtk3wFFuaaaY:Cjn4HsAiKV8sImst@192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?reconnect=true",
                        "jdbcUrl": "jdbc:mysql://192.168.0.97:3306/cf_0f5dda44_e678_4727_993f_30e6d455cc31?user=9vD0Mtk3wFFuaaaY&password=Cjn4HsAiKV8sImst"
                    }
                },
                {
                    "name": "mySql2",
                    "label": "p-mysql",
                    "tags": ["mysql","relational"],
                    "plan": "100mb-dev",
                    "credentials": {
                        "hostname": "192.168.0.97",
                        "port": 3306,
                        "name": "cf_b2d83697_5fa1_4a51_991b_975c9d7e5515",
                        "username": "gxXQb2pMbzFsZQW8",
                        "password": "lvMkGf6oJQvKSOwn",
                        "uri": "mysql://gxXQb2pMbzFsZQW8:lvMkGf6oJQvKSOwn@192.168.0.97:3306/cf_b2d83697_5fa1_4a51_991b_975c9d7e5515?reconnect=true",
                        "jdbcUrl": "jdbc:mysql://192.168.0.97:3306/cf_b2d83697_5fa1_4a51_991b_975c9d7e5515?user=gxXQb2pMbzFsZQW8&password=lvMkGf6oJQvKSOwn"
                    }
                }]
            }
            """;

        using Stream stream = CloudFoundryConfigurationProvider.GetStream(environment);
        var provider = new JsonStreamConfigurationProvider(new JsonStreamConfigurationSource(stream));
        provider.Load();

        provider.TryGet("p-config-server:0:name", out string? value).Should().BeTrue();
        value.Should().Be("myConfigServer");

        provider.TryGet("p-config-server:0:credentials:uri", out value).Should().BeTrue();
        value.Should().Be("https://config-eafc353b-77e2-4dcc-b52a-25777e996ed9.apps.test-cloud.com");

        provider.TryGet("p-service-registry:0:name", out value).Should().BeTrue();
        value.Should().Be("myServiceRegistry");

        provider.TryGet("p-service-registry:0:credentials:uri", out value).Should().BeTrue();
        value.Should().Be("https://eureka-f4b98d1c-3166-4741-b691-79abba5b2d51.apps.test-cloud.com");

        provider.TryGet("p-mysql:1:name", out value).Should().BeTrue();
        value.Should().Be("mySql2");

        provider.TryGet("p-mysql:1:credentials:uri", out value).Should().BeTrue();
        value.Should().Be("mysql://gxXQb2pMbzFsZQW8:lvMkGf6oJQvKSOwn@192.168.0.97:3306/cf_b2d83697_5fa1_4a51_991b_975c9d7e5515?reconnect=true");
    }
}
