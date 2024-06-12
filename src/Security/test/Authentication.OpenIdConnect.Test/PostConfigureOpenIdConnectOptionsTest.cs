// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Xunit;

namespace Steeltoe.Security.Authentication.OpenIdConnect.Test;

public sealed class PostConfigureOpenIdConnectOptionsTest
{
    [Fact]
    public void PostConfigure_AddsClientIdToValidAudiences()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "Authentication:Schemes:OpenIdConnect:Authority", "https://authority.com" },
            { "Authentication:Schemes:OpenIdConnect:ClientId", "testClient" }
        };

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(configuration);
        serviceCollection.AddAuthentication().AddOpenIdConnect();

        OpenIdConnectOptions openIdConnectOptions = serviceCollection.BuildServiceProvider().GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);

        var postConfigurer = new PostConfigureOpenIdConnectOptions();

        postConfigurer.PostConfigure(OpenIdConnectDefaults.AuthenticationScheme, openIdConnectOptions);

        openIdConnectOptions.TokenValidationParameters.ValidAudience.Should().Be("testClient");
    }

    [Fact]
    public void PostConfigure_ConfiguresForCloudFoundry()
    {
        const string vcapServices = """
            {
                "p-identity": [
                {
                    "label": "p-identity",
                    "provider": null,
                    "plan": "steeltoe",
                    "name": "mySSOService",
                    "tags": [],
                    "instance_guid": "ea8b8ac0-ce85-4726-8b39-d1b2eb55b45b",
                    "instance_name": "mySSOService",
                    "binding_guid": "be94e8e7-9246-49af-935f-5390ff10ac23",
                    "binding_name": null,
                    "credentials": {
                        "auth_domain": "https://steeltoe.login.sys.cf-app.com",
                        "grant_types": [ "authorization_code", "client_credentials" ],
                        "client_secret": "dd2c82e1-aa99-4eaf-9871-2eb7412b79bb",
                        "client_id": "4e6f8e34-f42b-440e-a042-f2b13c1d5bed"
                    },
                    "syslog_drain_url": null,
                    "volume_mounts": []
                }]
            }
            """;

        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);
        IConfigurationRoot configuration = new ConfigurationBuilder().AddCloudFoundryServiceBindings().Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(configuration);
        serviceCollection.AddAuthentication().AddOpenIdConnect();
        serviceCollection.ConfigureOpenIdConnectForCloudFoundry();

        OpenIdConnectOptions openIdConnectOptions = serviceCollection.BuildServiceProvider().GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(OpenIdConnectDefaults.AuthenticationScheme);

        openIdConnectOptions.Authority.Should().Be("https://steeltoe.login.sys.cf-app.com");
        openIdConnectOptions.MetadataAddress.Should().Be("https://steeltoe.login.sys.cf-app.com/.well-known/openid-configuration");
        openIdConnectOptions.RequireHttpsMetadata.Should().BeTrue();
        openIdConnectOptions.TokenValidationParameters.ValidIssuer.Should().Be("https://steeltoe.login.sys.cf-app.com/oauth/token");
        openIdConnectOptions.TokenValidationParameters.IssuerSigningKeyResolver.Should().NotBeNull();
        openIdConnectOptions.TokenValidationParameters.ValidAudience.Should().Be("4e6f8e34-f42b-440e-a042-f2b13c1d5bed");
    }
}
