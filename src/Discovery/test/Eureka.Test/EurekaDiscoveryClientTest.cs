// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaDiscoveryClientTest : AbstractBaseTest
{
    [Fact]
    public async Task Constructor_Initializes_Correctly()
    {
        var clientOptions = new EurekaClientOptions
        {
            ShouldRegisterWithEureka = false,
            ShouldFetchRegistry = false
        };

        var clientOptionsMonitor = new TestOptionsMonitor<EurekaClientOptions>(clientOptions);

        var instanceOptions = new EurekaInstanceOptions();
        var instanceOptionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>(instanceOptions);

        var appManager = new EurekaApplicationInfoManager(instanceOptionsMonitor, NullLogger<EurekaApplicationInfoManager>.Instance);
        var client = new EurekaDiscoveryClient(clientOptionsMonitor, instanceOptionsMonitor, appManager, NullLoggerFactory.Instance);

        Assert.NotNull(client.ClientOptions);
        Assert.Equal(clientOptions, client.ClientOptions);
        Assert.NotNull(client.HttpClient);
        Assert.NotNull(client.Description);

        IList<string> services = await client.GetServiceIdsAsync(CancellationToken.None);
        Assert.NotNull(services);
        Assert.Empty(services);

        IServiceInstance thisService = client.GetLocalServiceInstance();
        Assert.NotNull(thisService);
        Assert.Equal(instanceOptions.ResolveHostName(false), thisService.Host);
        Assert.Equal(instanceOptions.IsSecurePortEnabled, thisService.IsSecure);
        Assert.NotNull(thisService.Metadata);
        Assert.Equal(instanceOptions.NonSecurePort, thisService.Port);
        Assert.Equal(instanceOptions.AppName, thisService.ServiceId);
        Assert.NotNull(thisService.Uri);
        string scheme = instanceOptions.IsSecurePortEnabled ? "https" : "http";
        int uriPort = instanceOptions.IsSecurePortEnabled ? instanceOptions.SecurePort : instanceOptions.NonSecurePort;
        var uri = new Uri($"{scheme}://{instanceOptions.ResolveHostName(false)}:{uriPort}");
        Assert.Equal(uri, thisService.Uri);
    }
}
