﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;

namespace Steeltoe.Management.Endpoint.Trace.Test
{
    internal class MyIdentity : IIdentity
    {
        public string Name { get; } = "MyTestName";

        public string AuthenticationType { get; } = "MyTestAuthType";

        public bool IsAuthenticated { get; } = true;
    }
}
