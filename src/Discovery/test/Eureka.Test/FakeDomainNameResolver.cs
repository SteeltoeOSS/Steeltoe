// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Steeltoe.Common.Net;

namespace Steeltoe.Discovery.Eureka.Test;

/// <summary>
/// Avoids real DNS lookups, which are slow in tests when the host names can't be resolved.
/// </summary>
internal sealed class FakeDomainNameResolver : IDomainNameResolver
{
    public const string TestIPAddress = "10.20.30.40";
    public const string TestHostName = "test.domain-name.com";

    public IPAddress ResolveHostAddress(string hostName, bool throwOnError = false)
    {
        return IPAddress.Parse(TestIPAddress);
    }

    public string ResolveHostName(bool throwOnError = false)
    {
        return TestHostName;
    }
}
