// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Net;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

internal sealed class FakeDomainNameResolver : IDomainNameResolver
{
    // On macOS build servers, the call to Dns.GetHostName() takes roughly 5 seconds.
    // This slows down test runs and makes timing-based tests unreliable.

    public const string IPAddress = "127.1.2.3";
    public const string HostName = "dns-host-name";

    public string ResolveHostAddress(string hostName)
    {
        return IPAddress;
    }

    public string ResolveHostName(bool throwOnError = false)
    {
        return HostName;
    }
}
