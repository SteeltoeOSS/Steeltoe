// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.﻿

using Steeltoe.Security.Authentication.CloudFoundry;
using Steeltoe.Security.Authentication.CloudFoundry.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundryCore.Test
{
    public class CloudFoundryOAuthBuilderTest
    {
        [Fact]
        public async Task ShouldKeepDefaultServiceUrlsIfAuthDomainNotPresent()
        {
            var expectedAuthoricationUrl = $"http://{CloudFoundryDefaults.OAuthServiceUrl}/oauth/authorize";
            var webApplicationFactory = new TestApplicationFactory<TestServerStartup>();
#if NETCOREAPP3_1 || NET5_0
            var client = webApplicationFactory.CreateDefaultClient();
#else
            var client = webApplicationFactory.GetTestServer().CreateClient();
#endif
            var result = await client.GetAsync("http://localhost/");
            var location = result.Headers.Location.ToString();

            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
            Assert.StartsWith(expectedAuthoricationUrl, location, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ShouldAddAuthDomainToServiceUrlsIfPresent()
        {
            var authDomain = "http://this-config-server-url";
            var expectedAuthorizationUrl = $"{authDomain}/oauth/authorize";
            var expectedClientId = Guid.NewGuid().ToString();

            var configuration = new Dictionary<string, string>
            {
                { "security:oauth2:client:authDomain", authDomain },
                { "security:oauth2:client:clientId", expectedClientId },
                { "security:oauth2:client:clientSecret", Guid.NewGuid().ToString() }
            };

            var webApplicationFactory = new TestApplicationFactory<TestServerStartup>(configuration);

#if NETCOREAPP3_1 || NET5_0
            var client = webApplicationFactory.CreateDefaultClient();
#else
            var client = webApplicationFactory.GetTestServer().CreateClient();
#endif
            var result = await client.GetAsync("http://localhost/");
            var location = result.Headers.Location.ToString();

            Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
            Assert.StartsWith(expectedAuthorizationUrl, location, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"client_id={expectedClientId}", location);
        }
    }
}
