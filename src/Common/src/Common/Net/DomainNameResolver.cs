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
    /// Get the first listed <see cref="AddressFamily.InterNetwork" /> for the hostname.
    /// </summary>
    /// <param name="hostName">
    /// The hostname or address to use.
    /// </param>
    /// <returns>
    /// A string representation of the IP Address, or <see langword="null" />.
    /// </returns>
    public string? ResolveHostAddress(string hostName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);

        try
        {
            IPAddress[] hostAddresses = Dns.GetHostAddresses(hostName);
            return Array.Find(hostAddresses, ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();
        }
        catch (Exception)
        {
            // Ignore
            return null;
        }
    }

    public string? ResolveHostName(bool throwOnError = false)
    {
        string? resultingHostName = null;
        string? resultingHostEntryHostName = null;

        try
        {
            string hostName = Dns.GetHostName();
            resultingHostName = hostName;

            if (string.IsNullOrEmpty(hostName))
            {
                // Workaround for failure when running on macOS.
                // See https://github.com/actions/runner-images/issues/1335 and https://github.com/dotnet/runtime/issues/36849.

                throw new InvalidOperationException($"Dns.GetHostName is {GetTextFor(resultingHostName)}.");
            }

            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            resultingHostEntryHostName = hostEntry.HostName;

            if (string.IsNullOrEmpty(resultingHostEntryHostName))
            {
                throw new InvalidOperationException($"IPHostEntry.HostName is {GetTextFor(resultingHostEntryHostName)}.");
            }

            return resultingHostEntryHostName;
        }
        catch (Exception exception)
        {
            if (throwOnError)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve hostname. First={GetTextFor(resultingHostName)}, Second={GetTextFor(resultingHostEntryHostName)}", exception);
            }

            return null;
        }
    }

    private static string GetTextFor(string? value)
    {
        return value == null ? "null" : "empty";
    }
}
