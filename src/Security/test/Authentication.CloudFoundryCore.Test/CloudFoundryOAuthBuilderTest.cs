// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public class CloudFoundryOAuthBuilderTest
{
    [Fact]
    public async Task ShouldKeepDefaultServiceUrlsIfAuthDomainNotPresent()
    {
        string expectedAuthorizationUrl = $"http://{CloudFoundryDefaults.OAuthServiceUrl}/oauth/authorize";
        using var webApplicationFactory = new TestApplicationFactory<TestServerStartup>();
        HttpClient client = webApplicationFactory.CreateDefaultClient();
        HttpResponseMessage result = await client.GetAsync("http://localhost/");
        string location = result.Headers.Location.ToString();

        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.StartsWith(expectedAuthorizationUrl, location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShouldAddAuthDomainToServiceUrlsIfPresent()
    {
        string authDomain = "http://this-config-server-url";
        string expectedAuthorizationUrl = $"{authDomain}/oauth/authorize";
        string expectedClientId = Guid.NewGuid().ToString();

        var configuration = new Dictionary<string, string>
        {
            { "security:oauth2:client:authDomain", authDomain },
            { "security:oauth2:client:clientId", expectedClientId },
            { "security:oauth2:client:clientSecret", Guid.NewGuid().ToString() }
        };

        using var webApplicationFactory = new TestApplicationFactory<TestServerStartup>(configuration);
        HttpClient client = webApplicationFactory.CreateDefaultClient();
        HttpResponseMessage result = await client.GetAsync("http://localhost/");
        string location = result.Headers.Location.ToString();

        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.StartsWith(expectedAuthorizationUrl, location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"client_id={expectedClientId}", location);
    }
}
