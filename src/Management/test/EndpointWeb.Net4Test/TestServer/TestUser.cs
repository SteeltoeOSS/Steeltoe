// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;

namespace Steeltoe.Management.EndpointWeb.Test
{
    [Serializable]
    public class TestUser : IPrincipal
    {
        public IIdentity Identity =>
            new ClaimsIdentity(new List<Claim> { new Claim("scope", "actuator.read") }, "testAuth");

        public bool IsInRole(string role)
        {
            throw new NotImplementedException();
        }
    }
}
