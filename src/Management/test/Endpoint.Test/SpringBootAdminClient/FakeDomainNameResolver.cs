// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Steeltoe.Common.Net;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

internal sealed class FakeDomainNameResolver : IDomainNameResolver
{
    public const string TestIPAddress = "127.1.2.3";
    public const string TestHostName = "dns-host-name";

    public bool ReturnsNull { get; set; }

    public IPAddress? ResolveHostAddress(string hostName, bool throwOnError = false)
    {
        return ReturnsNull ? null : IPAddress.Parse(TestIPAddress);
    }

    public string? ResolveHostName(bool throwOnError = false)
    {
        return ReturnsNull ? null : TestHostName;
    }
}
