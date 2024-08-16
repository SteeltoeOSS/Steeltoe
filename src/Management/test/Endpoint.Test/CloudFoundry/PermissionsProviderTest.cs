// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.CloudFoundry;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public sealed class PermissionsProviderTest : BaseTest
{
    [Fact]
    public void IsCloudFoundryRequest_ReturnsExpected()
    {
        Assert.True(PermissionsProvider.IsCloudFoundryRequest("/cloudfoundryapplication"));
        Assert.True(PermissionsProvider.IsCloudFoundryRequest("/cloudfoundryapplication/badpath"));
    }

    [Fact]
    public async Task GetPermissionsAsyncTest()
    {
        PermissionsProvider permissionsProvider = GetPermissionsProvider();
        SecurityResult result = await permissionsProvider.GetPermissionsAsync("testToken", CancellationToken.None);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetPermissionsTest()
    {
        PermissionsProvider permissionsProvider = GetPermissionsProvider();
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        var permissions = new Dictionary<string, object>
        {
            { "read_sensitive_data", true }
        };

        response.Content = JsonContent.Create(permissions);
        Permissions result = await permissionsProvider.GetPermissionsAsync(response, CancellationToken.None);
        Assert.Equal(Permissions.Full, result);
    }

    private static PermissionsProvider GetPermissionsProvider()
    {
        IOptionsMonitor<CloudFoundryEndpointOptions> optionsMonitor = GetOptionsMonitorFromSettings<CloudFoundryEndpointOptions>();

        var services = new ServiceCollection();
        services.AddCloudFoundrySecurity();
        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        return new PermissionsProvider(optionsMonitor, httpClientFactory, NullLogger<PermissionsProvider>.Instance);
    }
}
