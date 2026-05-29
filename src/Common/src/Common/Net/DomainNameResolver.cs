// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;

namespace Steeltoe.Common.Net;

internal sealed class DomainNameResolver : IDomainNameResolver
{
    private static readonly bool IsInDiagnosticsMode = Environment.GetEnvironmentVariable("STEELTOE_MACOS_DIAGNOSE_HOSTNAME_LOOKUP") == "true";

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
        // Gather diagnostic information to investigate intermittent failures on macOS.
        string? resultFromGetHostName = null;
        string? resultFromGetHostEntry = null;
        bool? workaroundApplied = null;

        try
        {
            string hostName = Dns.GetHostName();
            resultFromGetHostName = hostName;

            if (string.IsNullOrEmpty(hostName))
            {
                // Workaround for failure when running on macOS.
                // See https://github.com/actions/runner-images/issues/1335 and https://github.com/dotnet/runtime/issues/36849.

                hostName = "localhost";
                workaroundApplied = true;
            }

            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            resultFromGetHostEntry = hostEntry.HostName;

            if (IsInDiagnosticsMode && string.IsNullOrEmpty(resultFromGetHostEntry))
            {
                throw new InvalidOperationException($"IPHostEntry.HostName returned {GetTextFor(resultFromGetHostEntry)}.");
            }

            return resultFromGetHostEntry;
        }
        catch (Exception exception)
        {
            if (IsInDiagnosticsMode)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve hostname. GetHostName={GetTextFor(resultFromGetHostName)}, GetHostEntry={GetTextFor(resultFromGetHostEntry)}, WorkaroundApplied={workaroundApplied}",
                    exception);
            }

            if (throwOnError)
            {
                throw;
            }

            return null;
        }
    }

    private static string GetTextFor(string? value)
    {
        if (value == null)
        {
            return "(null)";
        }

        return value.Length == 0 ? "(empty)" : value;
    }
}
