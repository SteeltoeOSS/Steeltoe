// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaApplicationInfoManagerTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Initializes_Correctly()
    {
        var instOptions = new EurekaInstanceOptions();
        var wrap = new TestOptionMonitorWrapper<EurekaInstanceOptions>(instOptions);
        var mgr = new EurekaApplicationInfoManager(wrap);
        Assert.Equal(instOptions, mgr.InstanceConfig);
    }
}
