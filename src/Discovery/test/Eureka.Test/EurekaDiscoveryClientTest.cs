// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaDiscoveryClientTest : AbstractBaseTest
{
    [Fact]
    public async Task Constructor_Initializes_Correctly()
    {
        var clientConfig = new EurekaClientOptions
        {
            ShouldRegisterWithEureka = false,
            ShouldFetchRegistry = false
        };

        var clientWrapper = new TestOptionMonitorWrapper<EurekaClientOptions>(clientConfig);

        var instanceConfig = new EurekaInstanceOptions();
        var instanceWrapper = new TestOptionMonitorWrapper<EurekaInstanceOptions>(instanceConfig);

        var appMgr = new EurekaApplicationInfoManager(instanceWrapper);
        var client = new EurekaDiscoveryClient(clientWrapper, instanceWrapper, appMgr);

        Assert.NotNull(client.ClientOptions);
        Assert.Equal(clientConfig, client.ClientOptions);
        Assert.NotNull(client.HttpClient);
        Assert.NotNull(client.Description);

        IList<string> services = await client.GetServiceIdsAsync(CancellationToken.None);
        Assert.NotNull(services);
        Assert.Empty(services);

        IServiceInstance thisService = client.GetLocalServiceInstance();
        Assert.NotNull(thisService);
        Assert.Equal(instanceConfig.ResolveHostName(false), thisService.Host);
        Assert.Equal(instanceConfig.IsSecurePortEnabled, thisService.IsSecure);
        Assert.NotNull(thisService.Metadata);
        Assert.Equal(instanceConfig.NonSecurePort, thisService.Port);
        Assert.Equal(instanceConfig.AppName, thisService.ServiceId);
        Assert.NotNull(thisService.Uri);
        string scheme = instanceConfig.IsSecurePortEnabled ? "https" : "http";
        int uriPort = instanceConfig.IsSecurePortEnabled ? instanceConfig.SecurePort : instanceConfig.NonSecurePort;
        var uri = new Uri($"{scheme}://{instanceConfig.ResolveHostName(false)}:{uriPort}");
        Assert.Equal(uri, thisService.Uri);
    }
}
