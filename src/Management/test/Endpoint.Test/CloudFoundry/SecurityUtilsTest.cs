// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public sealed class SecurityUtilsTest : BaseTest
{
    [Fact]
    public void IsCloudFoundryRequest_ReturnsExpected()
    {
        Assert.True(SecurityUtils.IsCloudFoundryRequest("/cloudfoundryapplication"));
        Assert.True(SecurityUtils.IsCloudFoundryRequest("/cloudfoundryapplication/badpath"));
    }

    [Fact]
    public async Task GetPermissionsAsyncTest()
    {
        SecurityUtils securityUtils = GetSecurityUtils();
        SecurityResult result = await securityUtils.GetPermissionsAsync("testToken", CancellationToken.None);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetPermissionsTest()
    {
        SecurityUtils securityUtils = GetSecurityUtils();
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        var perms = new Dictionary<string, object>
        {
            { "read_sensitive_data", true }
        };

        response.Content = JsonContent.Create(perms);
        Permissions result = await securityUtils.GetPermissionsAsync(response, CancellationToken.None);
        Assert.Equal(Permissions.Full, result);
    }

    private static SecurityUtils GetSecurityUtils()
    {
        var options = GetOptionsFromSettings<CloudFoundryEndpointOptions>();
        return new SecurityUtils(options, NullLogger.Instance);
    }
}
