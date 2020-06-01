// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.EndpointBase.Test.CloudFoundry
{
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
    }
}
