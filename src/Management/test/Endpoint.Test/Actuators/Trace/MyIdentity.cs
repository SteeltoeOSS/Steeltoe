// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Trace;

internal sealed class MyIdentity : IIdentity
{
    public string Name => "MyTestName";
    public string AuthenticationType => "MyTestAuthType";
    public bool IsAuthenticated => true;
}
