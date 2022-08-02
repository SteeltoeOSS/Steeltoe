// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;

namespace Steeltoe.Common.Net;

public static class DnsTools
{
    /// <summary>
    /// Get the first listed <see cref="AddressFamily.InterNetwork" /> for the host name.
    /// </summary>
    /// <param name="hostName">
    /// The host name or address to use.
    /// </param>
    /// <returns>
    /// String representation of the IP Address or <see langword="null" />.
    /// </returns>
    public static string ResolveHostAddress(string hostName)
    {
        try
        {
            return Dns.GetHostAddresses(hostName).FirstOrDefault(ip => ip.AddressFamily.Equals(AddressFamily.InterNetwork))?.ToString();
        }
        catch (Exception)
        {
            // Ignore
            return null;
        }
    }

    public static string ResolveHostName()
    {
        string result = null;

        try
        {
            result = Dns.GetHostName();

            if (!string.IsNullOrEmpty(result))
            {
                IPHostEntry response = Dns.GetHostEntry(result);

                if (response != null)
                {
                    return response.HostName;
                }
            }
        }
        catch (Exception)
        {
            // Ignore
        }

        return result;
    }
}
