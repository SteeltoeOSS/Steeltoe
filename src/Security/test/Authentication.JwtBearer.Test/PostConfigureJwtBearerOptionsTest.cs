// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;

namespace Steeltoe.Security.Authentication.JwtBearer.Test;

public sealed class PostConfigureJwtBearerOptionsTest
{
    [Fact]
    public void PostConfigure_AddsClientIdToValidAudiences()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "Authentication:Schemes:Bearer:ClientId", "testClient" }
        };

        var jwtBearerOptions = new JwtBearerOptions
        {
            Backchannel = new HttpClient()
        };

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var postConfigurer = new PostConfigureJwtBearerOptions(configuration, null!);

        postConfigurer.PostConfigure(null, jwtBearerOptions);

        jwtBearerOptions.TokenValidationParameters.ValidAudiences.Should().Contain("testClient");
    }

    [Fact]
    public void PostConfigure_ConfiguresForLocalUAA()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder().Build();

        var jwtBearerOptions = new JwtBearerOptions
        {
            Authority = SteeltoeSecurityDefaults.LocalUAAPath,
            Backchannel = new HttpClient()
        };

        jwtBearerOptions.RequireHttpsMetadata.Should().BeTrue();
        jwtBearerOptions.TokenValidationParameters.IssuerSigningKeyResolver.Should().BeNull();

        var postConfigure = new PostConfigureJwtBearerOptions(configuration, null!);

        postConfigure.PostConfigure(null, jwtBearerOptions);

        jwtBearerOptions.RequireHttpsMetadata.Should().BeFalse();
        jwtBearerOptions.TokenValidationParameters.ValidIssuer.Should().Be($"{SteeltoeSecurityDefaults.LocalUAAPath}/uaa/oauth/token");
        jwtBearerOptions.TokenValidationParameters.IssuerSigningKeyResolver.Should().NotBeNull();
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
                        "grant_types": [ "client_credentials" ],
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
        serviceCollection.ConfigureJwtBearerForCloudFoundry();
        serviceCollection.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        JwtBearerOptions jwtBearerOptions = serviceCollection.BuildServiceProvider().GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtBearerOptions.Authority.Should().Be("https://steeltoe.login.sys.cf-app.com");
        jwtBearerOptions.MetadataAddress.Should().Be("https://steeltoe.login.sys.cf-app.com/.well-known/openid-configuration");
        jwtBearerOptions.RequireHttpsMetadata.Should().BeTrue();
        jwtBearerOptions.TokenValidationParameters.ValidIssuer.Should().Be("https://steeltoe.login.sys.cf-app.com/oauth/token");
        jwtBearerOptions.TokenValidationParameters.IssuerSigningKeyResolver.Should().NotBeNull();
        jwtBearerOptions.TokenValidationParameters.ValidAudiences.Should().Contain("4e6f8e34-f42b-440e-a042-f2b13c1d5bed");
    }
}
