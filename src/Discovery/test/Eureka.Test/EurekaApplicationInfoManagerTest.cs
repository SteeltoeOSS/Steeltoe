// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaApplicationInfoManagerTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Initializes_Correctly()
    {
        var instanceOptions = new EurekaInstanceOptions();
        var instanceOptionsMonitor = new TestOptionsMonitor<EurekaInstanceOptions>(instanceOptions);
        var appManager = new EurekaApplicationInfoManager(instanceOptionsMonitor);
        Assert.Equal(instanceOptions, appManager.InstanceOptions);
    }
}
