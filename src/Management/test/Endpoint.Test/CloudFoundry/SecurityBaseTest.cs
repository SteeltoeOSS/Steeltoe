// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Options;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public class SecurityBaseTest : BaseTest
{
    [Fact]
    public void IsCloudFoundryRequest_ReturnsExpected()
    {
        SecurityBase securityBase = GetSecurityBase(out _, out _);

        Assert.True(securityBase.IsCloudFoundryRequest("/cloudfoundryapplication"));
        Assert.True(securityBase.IsCloudFoundryRequest("/cloudfoundryapplication/badpath"));
    }

    private static SecurityBase GetSecurityBase(out CloudFoundryEndpointOptions cloudOpts, out ManagementEndpointOptions managementOptions)
    {
        cloudOpts = GetOptionsFromSettings<CloudFoundryEndpointOptions>();
        managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>().Get(EndpointContextNames.CFManagemementOptionName);
        var securityBase = new SecurityBase(cloudOpts, managementOptions);
        return securityBase;
    }

    [Fact]
    public async Task GetPermissionsAsyncTest()
    {
        var securityBase = GetSecurityBase(out var cloudOpts, out var managementOptions);
        SecurityResult result = await securityBase.GetPermissionsAsync("testToken");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetPermissionsTest()
    {
        var securityBase = GetSecurityBase(out var cloudOpts, out var managementOptions);
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        var perms = new Dictionary<string, object>
        {
            { "read_sensitive_data", true }
        };

        response.Content = JsonContent.Create(perms);
        Permissions result = await securityBase.GetPermissionsAsync(response);
        Assert.Equal(Permissions.Full, result);
    }
}
