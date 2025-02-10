// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

public sealed class HeapDumpEndpointOptionsTest : BaseTest
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        var options = GetOptionsFromSettings<HeapDumpEndpointOptions>();
        Assert.Null(options.Enabled);
        Assert.Equal("heapdump", options.Id);
    }

    [Fact]
    public void Constructor_BindsConfigurationCorrectly()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:heapDump:enabled"] = "true",
            ["management:endpoints:cloudfoundry:validateCertificates"] = "true",
            ["management:endpoints:cloudfoundry:enabled"] = "true"
        };

        var heapDumpEndpointOptions = GetOptionsFromSettings<HeapDumpEndpointOptions>(appSettings);
        var cloudFoundryEndpointOptions = GetOptionsFromSettings<CloudFoundryEndpointOptions>(appSettings);

        Assert.True(cloudFoundryEndpointOptions.Enabled);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Id);
        Assert.Equal(string.Empty, cloudFoundryEndpointOptions.Path);
        Assert.True(cloudFoundryEndpointOptions.ValidateCertificates);

        Assert.True(heapDumpEndpointOptions.Enabled);
        Assert.Equal("heapdump", heapDumpEndpointOptions.Id);
        Assert.Equal("heapdump", heapDumpEndpointOptions.Path);

        if (Platform.IsOSX)
        {
            Assert.Equal("gcdump", heapDumpEndpointOptions.HeapDumpType);
        }
        else
        {
            Assert.Null(heapDumpEndpointOptions.HeapDumpType);
        }
    }
}
