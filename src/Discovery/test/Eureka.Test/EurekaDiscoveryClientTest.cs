// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaDiscoveryClientTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Initializes_Correctly()
    {
        var clientConfig = new EurekaClientOptions()
        {
            ShouldRegisterWithEureka = false,
            ShouldFetchRegistry = false
        };

        var cwrapper = new TestOptionMonitorWrapper<EurekaClientOptions>(clientConfig);

        var instConfig = new EurekaInstanceOptions();
        var iwrapper = new TestOptionMonitorWrapper<EurekaInstanceOptions>(instConfig);

        var appMgr = new EurekaApplicationInfoManager(iwrapper);
        var client = new EurekaDiscoveryClient(cwrapper, iwrapper, appMgr);

        Assert.NotNull(client.ClientConfig);
        Assert.Equal(clientConfig, client.ClientConfig);
        Assert.NotNull(client.HttpClient);
        Assert.NotNull(client.Description);
        Assert.NotNull(client.Services);
        Assert.Empty(client.Services);

        var thisService = client.GetLocalServiceInstance();
        Assert.NotNull(thisService);
        Assert.Equal(instConfig.GetHostName(false), thisService.Host);
        Assert.Equal(instConfig.SecurePortEnabled, thisService.IsSecure);
        Assert.NotNull(thisService.Metadata);
        Assert.Equal(instConfig.NonSecurePort, thisService.Port);
        Assert.Equal(instConfig.AppName, thisService.ServiceId);
        Assert.NotNull(thisService.Uri);
        var scheme = instConfig.SecurePortEnabled ? "https" : "http";
        var uriPort = instConfig.SecurePortEnabled ? instConfig.SecurePort : instConfig.NonSecurePort;
        var uri = new Uri(scheme + "://" + instConfig.GetHostName(false) + ":" + uriPort.ToString());
        Assert.Equal(uri, thisService.Uri);
    }
}