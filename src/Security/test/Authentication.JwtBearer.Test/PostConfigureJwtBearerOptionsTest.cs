// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;

namespace Steeltoe.Security.Authentication.JwtBearer.Test;

public sealed class PostConfigureJwtBearerOptionsTest
{
    [Fact]
    public void PostConfigure_AddsClientIdToValidAudiences()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Authentication:Schemes:Bearer:ClientId"] = "testClient"
        };

        using var httpClient = new HttpClient();

        var jwtBearerOptions = new JwtBearerOptions
        {
            Backchannel = httpClient
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).BuildAsRoot();
        using var resolver = new TokenKeyResolver(TimeProvider.System, NullLoggerFactory.Instance);
        var postConfigurer = new PostConfigureJwtBearerOptions(configurationRoot, resolver);

        postConfigurer.PostConfigure(null, jwtBearerOptions);

        jwtBearerOptions.TokenValidationParameters.ValidAudiences.Should().Contain("testClient");
    }

    [Fact]
    public void PostConfigure_ResolvesSigningKeysFromTokenKeysEndpointOfAuthority()
    {
        const string keySet = """
            {
              "keys": [
                {
                  "kid": "key-a",
                  "kty": "RSA",
                  "use": "sig",
                  "n": "AJPu8R9-W2AaabJ-eHAVvhZa73k3vty-MZe4sdd1yvHgXApjTsgYTc4ZLOraA4kY64MOUFu-nBYq1URSFUJImbFMA8hjmsd6vDTicgRV4qZSFEnK8fGiNI5GP2HeosQ",
                  "e": "AQAB"
                }
              ]
            }
            """;

        using var handler = new DelegateToMockHttpClientHandler();
        MockedRequest tokenKeysRequest = handler.Mock.When(HttpMethod.Get, "https://authority.example.com/token_keys").Respond("application/json", keySet);
        handler.RespondToAnyRequest(HttpStatusCode.NotFound);

        using var httpClient = new HttpClient(handler);

        var jwtBearerOptions = new JwtBearerOptions
        {
            Authority = "https://authority.example.com",
            Backchannel = httpClient
        };

        using ConfigurationRoot configurationRoot = new ConfigurationBuilder().BuildAsRoot();
        using var resolver = new TokenKeyResolver(TimeProvider.System, NullLoggerFactory.Instance);
        var postConfigurer = new PostConfigureJwtBearerOptions(configurationRoot, resolver);

        postConfigurer.PostConfigure(null, jwtBearerOptions);

        TokenValidationParameters parameters = jwtBearerOptions.TokenValidationParameters;
        parameters.ValidIssuer.Should().Be("https://authority.example.com/oauth/token");
        parameters.IssuerSigningKeyResolver.Should().NotBeNull();

        IEnumerable<SecurityKey> keys = parameters.IssuerSigningKeyResolver(string.Empty, null, "key-a", parameters);

        keys.Should().ContainSingle().Which.KeyId.Should().Be("key-a");
        handler.Mock.GetMatchCount(tokenKeysRequest).Should().Be(1);

        parameters.IssuerSigningKeyResolver(string.Empty, null, "unknown-key", parameters).Should().BeEmpty();
    }

    [Fact]
    public async Task PostConfigure_ConfiguresForCloudFoundry()
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
                        "auth_domain": "https://steeltoe.uaa.sys.cf-app.com",
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
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCloudFoundryServiceBindings(CloudFoundryServiceBrokerTypes.Identity).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => configurationRoot);
        services.AddLogging();
        services.AddAuthentication().AddJwtBearer().ConfigureJwtBearerForCloudFoundry();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        JwtBearerOptions options = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

        options.Authority.Should().Be("https://steeltoe.uaa.sys.cf-app.com");
        options.MetadataAddress.Should().Be("https://steeltoe.uaa.sys.cf-app.com/.well-known/openid-configuration");
        options.RequireHttpsMetadata.Should().BeTrue();
        options.TokenValidationParameters.ValidIssuer.Should().Be("https://steeltoe.uaa.sys.cf-app.com/oauth/token");
        options.TokenValidationParameters.ValidIssuers.Should().BeEmpty();
        options.TokenValidationParameters.IssuerSigningKeyResolver.Should().NotBeNull();
        options.TokenValidationParameters.ValidAudiences.Should().Contain("4e6f8e34-f42b-440e-a042-f2b13c1d5bed");
    }

    [Fact]
    public async Task PostConfigure_ConfiguresForCloudFoundry_AllowMultipleIssuers()
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

        using var applicationScope = new EnvironmentVariableScope("VCAP_APPLICATION", "{}");
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCloudFoundryServiceBindings(CloudFoundryServiceBrokerTypes.Identity).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => configurationRoot);
        services.AddLogging();
        services.AddAuthentication().AddJwtBearer().ConfigureJwtBearerForCloudFoundry();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        JwtBearerOptions options = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

        options.Authority.Should().Be("https://steeltoe.login.sys.cf-app.com");
        options.MetadataAddress.Should().Be("https://steeltoe.login.sys.cf-app.com/.well-known/openid-configuration");
        options.RequireHttpsMetadata.Should().BeTrue();
        options.TokenValidationParameters.ValidIssuer.Should().BeNull();

        options.TokenValidationParameters.ValidIssuers.Should().Contain("https://steeltoe.login.sys.cf-app.com/oauth/token").And
            .Contain("https://steeltoe.uaa.sys.cf-app.com/oauth/token");

        options.TokenValidationParameters.IssuerSigningKeyResolver.Should().NotBeNull();
        options.TokenValidationParameters.ValidAudiences.Should().Contain("4e6f8e34-f42b-440e-a042-f2b13c1d5bed");
    }
}
