// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBinding.Test;

public sealed class CloudFoundryServiceBindingConfigurationProviderTest
{
    private const string VcapServicesJson = @"
    {
        ""p-config-server"": [{
            ""name"": ""myConfigServer"",
            ""label"": ""p-config-server"",
            ""tags"": [
                ""configuration"",
                ""spring-cloud""
            ],
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
        }],
        ""p-identity"": [{
          ""label"": ""p-identity"",
          ""provider"": null,
          ""plan"": ""uaa"",
          ""name"": ""mySSOService"",
          ""tags"": [],
          ""instance_guid"": ""f0fd571f-4aaf-4807-b34f-3777b053de2f"",
          ""instance_name"": ""mySSOService"",
          ""binding_guid"": ""c2d303a8-8d4f-48ce-916a-74c0305e30b2"",
          ""binding_name"": null,
          ""credentials"": {
            ""auth_domain"": ""https://login.system.testcloud.com"",
            ""grant_types"": [
              ""authorization_code"",
              ""client_credentials""
            ],
            ""client_secret"": ""81f92f37-a38f-4b5e-b769-4c933c5c5aca"",
            ""client_id"": ""c2d303a8-8d4f-48ce-916a-74c0305e30b2""
          },
          ""syslog_drain_url"": null,
          ""volume_mounts"": []
        }
      ]
    }";

    [Fact]
    public void PostProcessors_OnByDefault()
    {
        var postProcessor = new TestPostProcessor();

        var reader = new StringServiceBindingsReader(VcapServicesJson);
        var source = new CloudFoundryServiceBindingConfigurationSource(reader);
        source.RegisterPostProcessor(postProcessor);

        var builder = new ConfigurationBuilder();
        builder.Add(source);
        builder.Build();

        postProcessor.PostProcessorCalled.Should().BeTrue();
    }

    [Fact]
    public void Build_CapturesParentConfiguration()
    {
        var reader = new StringServiceBindingsReader(string.Empty);
        var source = new CloudFoundryServiceBindingConfigurationSource(reader);

        var builder = new ConfigurationBuilder();
        builder.Add(source);

        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "some:value:in:configuration:path", "true" }
        });

        builder.Build();

        IConfigurationRoot parentConfiguration = source.GetParentConfiguration();
        parentConfiguration.Should().NotBeNull();
        parentConfiguration.GetValue<bool>("some:value:in:configuration:path").Should().BeTrue();
    }

    [Fact]
    public void Build_LoadsServiceBindings()
    {
        var reader = new StringServiceBindingsReader(VcapServicesJson);
        var source = new CloudFoundryServiceBindingConfigurationSource(reader);

        var builder = new ConfigurationBuilder();
        builder.Add(source);

        IConfigurationRoot configurationRoot = builder.Build();
        IConfigurationSection section = configurationRoot.GetRequiredSection("vcap:services");

        section.GetValue<string>("p-config-server:0:name").Should().Be("myConfigServer");
        section.GetValue<string>("p-config-server:0:credentials:uri").Should().Be("https://config-eafc353b-77e2-4dcc-b52a-25777e996ed9.apps.testcloud.com");
        section.GetValue<string>("p-service-registry:0:name").Should().Be("myServiceRegistry");
        section.GetValue<string>("p-service-registry:0:credentials:uri").Should().Be("https://eureka-f4b98d1c-3166-4741-b691-79abba5b2d51.apps.testcloud.com");
        section.GetValue<string>("p-mysql:1:name").Should().Be("mySql2");
        section.GetValue<string>("p-identity:0:name").Should().Be("mySSOService");
        section.GetValue<string>("p-identity:0:credentials:auth_domain").Should().Be("https://login.system.testcloud.com");

        section.GetValue<string>("p-mysql:1:credentials:uri").Should()
            .Be("mysql://gxXQb2pMbzFsZQW8:lvMkGf6oJQvKSOwn@192.168.0.97:3306/cf_b2d83697_5fa1_4a51_991b_975c9d7e5515?reconnect=true");
    }

    private sealed class TestPostProcessor : IConfigurationPostProcessor
    {
        public bool PostProcessorCalled { get; private set; }

        public void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
        {
            PostProcessorCalled = true;
        }
    }
}
