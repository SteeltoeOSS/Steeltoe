// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Steeltoe.Common.Net;

namespace Steeltoe.Common.Test.Net;

internal sealed class FakeDomainNameResolver : IDomainNameResolver
{
    public IPAddress? HostAddress { get; set; }
    public Exception? ErrorInResolveHostAddress { get; set; }

    public string? HostName { get; set; }
    public Exception? ErrorInResolveHostName { get; set; }

    public IPAddress? ResolveHostAddress(string hostName, bool throwOnError = false)
    {
        if (throwOnError && ErrorInResolveHostAddress != null)
        {
            throw ErrorInResolveHostAddress;
        }

        return HostAddress;
    }

    public string? ResolveHostName(bool throwOnError = false)
    {
        if (throwOnError && ErrorInResolveHostName != null)
        {
            throw ErrorInResolveHostName;
        }

        return HostName;
    }
}
