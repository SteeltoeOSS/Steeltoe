// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;

namespace Steeltoe.Common.Net;

internal sealed class DomainNameResolver : IDomainNameResolver
{
    public static DomainNameResolver Instance { get; } = new();

    private DomainNameResolver()
    {
    }

    /// <summary>
    /// Get the first listed IPv4 address for the specified hostname.
    /// </summary>
    public IPAddress? ResolveHostAddress(string hostName, bool throwOnError = false)
    {
        ArgumentNullException.ThrowIfNull(hostName);

        try
        {
            return Dns.GetHostAddresses(hostName, AddressFamily.InterNetwork).FirstOrDefault();
        }
        catch (Exception)
        {
            if (throwOnError)
            {
                throw;
            }

            return null;
        }
    }

    public string? ResolveHostName(bool throwOnError = false)
    {
        try
        {
            // On macOS build servers, the call to Dns.GetHostName() takes roughly 5 seconds.
            // This slows down test runs and makes timing-based tests unreliable. Tests should use a fake/mock instead.

            string hostName = Dns.GetHostName();

            if (string.IsNullOrEmpty(hostName))
            {
                // Workaround for failure when running on macOS.
                // See https://github.com/actions/runner-images/issues/1335 and https://github.com/dotnet/runtime/issues/36849.
                hostName = "localhost";
            }

            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            return hostEntry.HostName;
        }
        catch (Exception)
        {
            if (throwOnError)
            {
                throw;
            }

            return null;
        }
    }
}
