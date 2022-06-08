// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryExtensionsTest
{
    private readonly string vcap_application = @"
            {
                ""cf_api"": ""https://api.system.testcloud.com"",
                ""limits"": { ""fds"": 16384, ""mem"": 512, ""disk"": 1024 },
                ""application_name"": ""fortuneService"",
                ""application_uris"": [ ""fortuneservice.apps.testcloud.com"" ],
                ""name"": ""fortuneService"",
                ""space_name"": ""test"",
                ""space_id"": ""54af9d15-2f18-453b-a533-f0c9e6522c97"",
                ""uris"": [ ""fortuneservice.apps.testcloud.com"" ],
                ""users"": null,
                ""application_id"": ""2ddeb650-187e-41db-a4a6-84fb60567908"",
                ""version"": ""d2911a1c-c81a-47aa-be81-d820a6700d2b"",
                ""application_version"": ""d2911a1c-c81a-47aa-be81-d820a6700d2b""
            }";

    private readonly string vcap_services = @"
            {
                ""user-provided"": [{
                    ""credentials"": {
                        ""client_id"": ""testApp"",
                        ""client_secret"": ""testApp"",
                        ""uri"": ""uaa://login.system.testcloud.com""
                    },
                    ""syslog_drain_url"": """",
                    ""volume_mounts"": [],
                    ""label"": ""user-provided"",
                    ""name"": ""myOAuthService"",
                    ""tags"": []
                }]
            }";

    private readonly string openIdConfigResponse = @"
                {
                    ""issuer"":""https://default_oauthserviceurl/oauth/token"",
                    ""authorization_endpoint"":""https://default_oauthserviceurl/oauth/authorize"",
                    ""token_endpoint"":""https://default_oauthserviceurl/oauth/token"",
                    ""token_endpoint_auth_methods_supported"":[""client_secret_basic"",""client_secret_post""],
                    ""token_endpoint_auth_signing_alg_values_supported"":[""RS256"",""HS256""],
                    ""userinfo_endpoint"":""https://default_oauthserviceurl/userinfo"",
                    ""jwks_uri"":""https://default_oauthserviceurl/token_keys"",
                    ""scopes_supported"":[""openid"",""profile"",""email"",""phone"",""roles"",""user_attributes""],
                    ""response_types_supported"":[""code"",""code id_token"",""id_token"",""token id_token""],
                    ""subject_types_supported"":[""public""],
                    ""id_token_signing_alg_values_supported"":[""RS256"",""HS256""],
                    ""id_token_encryption_alg_values_supported"":[""none""],
                    ""claim_types_supported"":[""normal""],
                    ""claims_supported"":[""sub"",""user_name"",""origin"",""iss"",""auth_time"",""amr"",""acr"",""client_id"",""aud"",""zid"",""grant_type"",""user_id"",""azp"",""scope"",""exp"",""iat"",""jti"",""rev_sig"",""cid"",""given_name"",""family_name"",""phone_number"",""email""],
                    ""claims_parameter_supported"":false,
                    ""service_documentation"":""http://docs.cloudfoundry.org/api/uaa/"",
                    ""ui_locales_supported"":[""en-US""]
                }";

    private readonly string jwksResponse = @"
            {
                ""keys"":[{
                    ""kty"":""RSA"",
                    ""e"":""AQAB"",
                    ""use"":""sig"",
                    ""kid"":""uaa-jwt-key-1"",
                    ""alg"":""RS256"",
                    ""value"":""-----BEGIN PUBLIC KEY-----\nMIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDHtC5gUXxBKpEqZTLkNvFwNGnN\nIkggNOwOQVNbpO0WVHIivig5L39WqS9u0hnA+O7MCA/KlrAR4bXaeVVhwfUPYBKI\npaaTWFQR5cTR1UFZJL/OF9vAfpOwznoD66DDCnQVpbCjtDYWX+x6imxn8HCYxhMo\nl6ZnTbSsFW6VZjFMjQIDAQAB\n-----END PUBLIC KEY-----"",
                    ""n"":""AMe0LmBRfEEqkSplMuQ28XA0ac0iSCA07A5BU1uk7RZUciK-KDkvf1apL27SGcD47swID8qWsBHhtdp5VWHB9Q9gEoilppNYVBHlxNHVQVkkv84X28B-k7DOegProMMKdBWlsKO0NhZf7HqKbGfwcJjGEyiXpmdNtKwVbpVmMUyN""
                }]
            }";

    [Fact]
    public async Task AddCloudFoundryOAuthAuthentication_AddsIntoPipeline()
    {
        var builder = GetHostBuilder<TestServerStartup>();
        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var result = await client.GetAsync("http://localhost/");
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        var location = result.Headers.Location.ToString();
        Assert.StartsWith("http://default_oauthserviceurl/oauth/authorize", location);
    }

    [Fact]
    public async Task AddCloudFoundryOAuthAuthentication_AddsIntoPipeline_UsesSSOInfo()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);

        var builder = GetHostBuilder<TestServerStartup>();
        using (var server = new TestServer(builder))
        {
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/");
            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
            var location = result.Headers.Location.ToString();
            Assert.StartsWith("https://login.system.testcloud.com/oauth/authorize", location);
        }

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public async Task AddCloudFoundryJwtBearerAuthentication_AddsIntoPipeline()
    {
        var builder = GetHostBuilder<TestServerJwtStartup>();
        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var result = await client.GetAsync("http://localhost/");
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task AddCloudFoundryOpenId_AddsIntoPipeline()
    {
        Environment.SetEnvironmentVariable("openIdConfigResponse", openIdConfigResponse);
        Environment.SetEnvironmentVariable("jwksResponse", jwksResponse);

        var builder = GetHostBuilder<TestServerOpenIdStartup>(new Dictionary<string, string> { { "security:oauth2:client:Timeout", "9999" } });
        using var server = new TestServer(builder);
        var client = server.CreateClient();
        var result = await client.GetAsync("http://localhost/");
        var body = await result.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        var location = result.Headers.Location.ToString();
        Assert.StartsWith("https://default_oauthserviceurl/oauth/authorize", location);
    }

    [Fact]
    public async Task AddCloudFoundryOpenId_AddsIntoPipeline_UsesSSOInfo()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);
        Environment.SetEnvironmentVariable("openIdConfigResponse", openIdConfigResponse.Replace("default_oauthserviceurl", "login.system.testcloud.com"));
        Environment.SetEnvironmentVariable("jwksResponse", jwksResponse);

        var builder = GetHostBuilder<TestServerOpenIdStartup>();
        using (var server = new TestServer(builder))
        {
            var client = server.CreateClient();
            var result = await client.GetAsync("http://localhost/");
            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
            var location = result.Headers.Location.ToString();
            Assert.StartsWith("https://login.system.testcloud.com/oauth/authorize", location);
        }

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    private IWebHostBuilder GetHostBuilder<T>(Dictionary<string, string> appsettings = null)
        where T : class
    {
        return new WebHostBuilder()
            .UseStartup<T>()
            .ConfigureAppConfiguration((context, builder) =>
            {
                if (appsettings is not null)
                {
                    builder.AddInMemoryCollection(appsettings);
                }

                builder.AddCloudFoundry();
            })
            .UseEnvironment("development");
    }
}
