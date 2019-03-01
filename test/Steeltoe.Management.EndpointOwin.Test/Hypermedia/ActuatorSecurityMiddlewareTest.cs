// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Primitives;
using Microsoft.Owin.Testing;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointOwin.Test;
using System;
using System.Net;
using Xunit;

namespace Steeltoe.Management.EndpointOwin.CloudFoundry.Test
{
    public class ActuatorSecurityMiddlewareTest : BaseTest
    {
        private readonly SecurityBase _base;

        public ActuatorSecurityMiddlewareTest(IManagementOptions mopts)
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", "somestuff");
            _base = new SecurityBase(new CloudFoundryEndpointOptions(), mopts);
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
