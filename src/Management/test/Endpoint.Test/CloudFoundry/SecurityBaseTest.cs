// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
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

    [Fact]
    public async Task GetPermissionsAsyncTest()
    {
        SecurityBase securityBase = GetSecurityBase(out CloudFoundryEndpointOptions cloudOpts, out ManagementEndpointOptions managementOptions);
        SecurityResult result = await securityBase.GetPermissionsAsync("testToken");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetPermissionsTest()
    {
        SecurityBase securityBase = GetSecurityBase(out CloudFoundryEndpointOptions cloudOpts, out ManagementEndpointOptions managementOptions);
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        var perms = new Dictionary<string, object>
        {
            { "read_sensitive_data", true }
        };

        response.Content = JsonContent.Create(perms);
        Permissions result = await securityBase.GetPermissionsAsync(response);
        Assert.Equal(Permissions.Full, result);
    }

    private static SecurityBase GetSecurityBase(out CloudFoundryEndpointOptions cloudOpts, out ManagementEndpointOptions managementOptions)
    {
        cloudOpts = GetOptionsFromSettings<CloudFoundryEndpointOptions>();
        managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>().Get(CFContext.Name);
        var securityBase = new SecurityBase(cloudOpts, managementOptions, NullLogger.Instance);
        return securityBase;
    }
}
