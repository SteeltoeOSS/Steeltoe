// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net;
using System.Net.Sockets;

namespace Steeltoe.Common.Net;

internal static class DnsTools
{
    /// <summary>
    /// Get the first listed <see cref="AddressFamily.InterNetwork" /> for the hostname.
    /// </summary>
    /// <param name="hostName">
    /// The hostname or address to use.
    /// </param>
    /// <returns>
    /// String representation of the IP Address or <see langword="null" />.
    /// </returns>
    public static string? ResolveHostAddress(string hostName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);

        try
        {
            return Array.Find(Dns.GetHostAddresses(hostName), ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
        }
        catch (Exception)
        {
            // Ignore
            return null;
        }
    }

    public static string? ResolveHostName(bool throwOnError = false)
    {
        try
        {
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
