// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net;
using Consul;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul;

/// <summary>
/// A factory to use in configuring and creating a Consul client.
/// </summary>
internal static class ConsulClientFactory
{
    /// <summary>
    /// Creates a Consul client using the provided configuration options.
    /// </summary>
    public static IConsulClient CreateClient(ConsulOptions options)
    {
        ArgumentGuard.NotNull(options);

        var client = new ConsulClient(configuration =>
        {
            configuration.Address = new Uri($"{options.Scheme}://{options.Host}:{options.Port}");
            configuration.Token = options.Token;
            configuration.Datacenter = options.Datacenter;

            if (!string.IsNullOrEmpty(options.WaitTime))
            {
                configuration.WaitTime = DateTimeConversions.ToTimeSpan(options.WaitTime);
            }

            if (!string.IsNullOrEmpty(options.Password) || !string.IsNullOrEmpty(options.Username))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                configuration.HttpAuth = new NetworkCredential(options.Username, options.Password);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        });

        return client;
    }
}
