// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.CloudFoundry;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public class SecurityBaseTest : BaseTest
{
    [Fact]
    public void IsCloudFoundryRequest_ReturnsExpected()
    {
        var cloudOpts = new CloudFoundryEndpointOptions();
        var mgmtOpts = new CloudFoundryManagementOptions();
        mgmtOpts.EndpointOptions.Add(cloudOpts);
        var securityBase = new SecurityBase(cloudOpts, mgmtOpts, null);

        Assert.True(securityBase.IsCloudFoundryRequest("/cloudfoundryapplication"));
        Assert.True(securityBase.IsCloudFoundryRequest("/cloudfoundryapplication/badpath"));
    }

    [Fact]
    public async Task GetPermissionsAsyncTest()
    {
        var cloudOpts = new CloudFoundryEndpointOptions();
        var mgmtOpts = new CloudFoundryManagementOptions();
        mgmtOpts.EndpointOptions.Add(cloudOpts);
        var securityBase = new SecurityBase(cloudOpts, mgmtOpts, null);
        var result = await securityBase.GetPermissionsAsync("testToken");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetPermissionsTest()
    {
        var cloudOpts = new CloudFoundryEndpointOptions();
        var mgmtOpts = new CloudFoundryManagementOptions();
        mgmtOpts.EndpointOptions.Add(cloudOpts);
        var securityBase = new SecurityBase(cloudOpts, mgmtOpts, null);
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        var perms = new Dictionary<string, object> { { "read_sensitive_data", true } };

        response.Content = JsonContent.Create(perms);
        var result = await securityBase.GetPermissions(response);
        Assert.Equal(Permissions.FULL, result);
    }
}