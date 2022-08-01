// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaDiscoveryClientTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Initializes_Correctly()
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

        Assert.NotNull(client.ClientConfig);
        Assert.Equal(clientConfig, client.ClientConfig);
        Assert.NotNull(client.HttpClient);
        Assert.NotNull(client.Description);
        Assert.NotNull(client.Services);
        Assert.Empty(client.Services);

        var thisService = client.GetLocalServiceInstance();
        Assert.NotNull(thisService);
        Assert.Equal(instanceConfig.GetHostName(false), thisService.Host);
        Assert.Equal(instanceConfig.SecurePortEnabled, thisService.IsSecure);
        Assert.NotNull(thisService.Metadata);
        Assert.Equal(instanceConfig.NonSecurePort, thisService.Port);
        Assert.Equal(instanceConfig.AppName, thisService.ServiceId);
        Assert.NotNull(thisService.Uri);
        var scheme = instanceConfig.SecurePortEnabled ? "https" : "http";
        var uriPort = instanceConfig.SecurePortEnabled ? instanceConfig.SecurePort : instanceConfig.NonSecurePort;
        var uri = new Uri($"{scheme}://{instanceConfig.GetHostName(false)}:{uriPort}");
        Assert.Equal(uri, thisService.Uri);
    }
}
