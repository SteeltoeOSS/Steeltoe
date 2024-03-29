// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Consul;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul;

/// <summary>
/// A factory to use in configuring and creating a Consul client.
/// </summary>
public static class ConsulClientFactory
{
    /// <summary>
    /// Create a Consul client using the provided configuration options.
    /// </summary>
    /// <param name="options">
    /// the configuration options.
    /// </param>
    /// <returns>
    /// a Consul client.
    /// </returns>
    public static IConsulClient CreateClient(ConsulOptions options)
    {
        ArgumentGuard.NotNull(options);

        var client = new ConsulClient(s =>
        {
            s.Address = new Uri($"{options.Scheme}://{options.Host}:{options.Port}");
            s.Token = options.Token;
            s.Datacenter = options.Datacenter;

            if (!string.IsNullOrEmpty(options.WaitTime))
            {
                s.WaitTime = DateTimeConversions.ToTimeSpan(options.WaitTime);
            }

            if (!string.IsNullOrEmpty(options.Password) || !string.IsNullOrEmpty(options.Username))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                s.HttpAuth = new NetworkCredential(options.Username, options.Password);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        });

        return client;
    }
}
