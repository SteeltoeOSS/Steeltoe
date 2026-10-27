// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using RichardSzalay.MockHttp;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;

namespace Steeltoe.Security.Authentication.OpenIdConnect.Test;

public sealed class PostConfigureOpenIdConnectOptionsTest
{
    [Fact]
    public async Task PostConfigure_AddsClientIdToValidAudiences()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Authentication:Schemes:OpenIdConnect:Authority"] = "https://authority.com",
            ["Authentication:Schemes:OpenIdConnect:ClientId"] = "testClient"
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => configurationRoot);
        services.AddAuthentication().AddOpenIdConnect();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
        OpenIdConnectOptions options = optionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);

        using var resolver = new TokenKeyResolver(TimeProvider.System, NullLoggerFactory.Instance);
        var postConfigurer = new PostConfigureOpenIdConnectOptions(resolver);

        postConfigurer.PostConfigure(OpenIdConnectDefaults.AuthenticationScheme, options);

        options.TokenValidationParameters.ValidAudience.Should().Be("testClient");
    }

    [Fact]
    public void PostConfigure_ConfiguresOpenIdConnectDefaults()
    {
        var options = new OpenIdConnectOptions();

        using var resolver = new TokenKeyResolver(TimeProvider.System, NullLoggerFactory.Instance);
        var postConfigurer = new PostConfigureOpenIdConnectOptions(resolver);

        postConfigurer.PostConfigure(OpenIdConnectDefaults.AuthenticationScheme, options);

        options.ResponseType.Should().Be(OpenIdConnectResponseType.Code);
        options.SignInScheme.Should().Be(CookieAuthenticationDefaults.AuthenticationScheme);
        options.TokenValidationParameters.NameClaimType.Should().Be("user_name");
        options.Events.OnTokenValidated.Should().NotBeNull();
    }

    [Fact]
    public async Task PostConfigure_MapsScopesFromAccessTokenToClaims()
    {
        var options = new OpenIdConnectOptions();

        using var resolver = new TokenKeyResolver(TimeProvider.System, NullLoggerFactory.Instance);
        var postConfigurer = new PostConfigureOpenIdConnectOptions(resolver);

        postConfigurer.PostConfigure(OpenIdConnectDefaults.AuthenticationScheme, options);

        var identity = new ClaimsIdentity("test");
        identity.AddClaim(new Claim("scope", "openid"));

        var scheme = new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler));

        var context = new TokenValidatedContext(new DefaultHttpContext(), scheme, options, new ClaimsPrincipal(identity), new AuthenticationProperties())
        {
            TokenEndpointResponse = new OpenIdConnectMessage
            {
                Scope = "openid profile email"
            }
        };

        await options.Events.OnTokenValidated(context);

        // Scopes that are already present as claims are not added again.
        identity.Claims.Where(claim => claim.Type == "scope").Select(claim => claim.Value).Should().Equal("openid", "profile", "email");
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

        var options = new OpenIdConnectOptions
        {
            Authority = "https://authority.example.com",
            Backchannel = httpClient
        };

        using var resolver = new TokenKeyResolver(TimeProvider.System, NullLoggerFactory.Instance);
        var postConfigurer = new PostConfigureOpenIdConnectOptions(resolver);

        postConfigurer.PostConfigure(OpenIdConnectDefaults.AuthenticationScheme, options);

        TokenValidationParameters parameters = options.TokenValidationParameters;
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
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCloudFoundryServiceBindings(CloudFoundryServiceBrokerTypes.Identity).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_ => configurationRoot);
        services.AddLogging();
        services.AddAuthentication().AddOpenIdConnect().ConfigureOpenIdConnectForCloudFoundry();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>();
        OpenIdConnectOptions options = optionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);

        options.Authority.Should().Be("https://steeltoe.login.sys.cf-app.com");
        options.MetadataAddress.Should().Be("https://steeltoe.login.sys.cf-app.com/.well-known/openid-configuration");
        options.RequireHttpsMetadata.Should().BeTrue();
        options.TokenValidationParameters.ValidIssuer.Should().Be("https://steeltoe.login.sys.cf-app.com/oauth/token");
        options.TokenValidationParameters.IssuerSigningKeyResolver.Should().NotBeNull();
        options.TokenValidationParameters.ValidAudience.Should().Be("4e6f8e34-f42b-440e-a042-f2b13c1d5bed");
    }
}
