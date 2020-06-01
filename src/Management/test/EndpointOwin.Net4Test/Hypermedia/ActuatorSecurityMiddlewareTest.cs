// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using System;

namespace Steeltoe.Management.EndpointOwin.CloudFoundry.Test
{
    public class ActuatorSecurityMiddlewareTest : BaseTest
    {
        public ActuatorSecurityMiddlewareTest(IManagementOptions mopts)
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "somestuff");
        }

        public override void Dispose()
        {
            base.Dispose();
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP__APPLICATION__APPLICATION_ID", null);
            Environment.SetEnvironmentVariable("management__endpoints__cloudfoundry__enabled", null);
            Environment.SetEnvironmentVariable("VCAP__APPLICATION__APPLICATION_ID", null);
            Environment.SetEnvironmentVariable("VCAP__APPLICATION__CF_API", null);
        }
    }
}
