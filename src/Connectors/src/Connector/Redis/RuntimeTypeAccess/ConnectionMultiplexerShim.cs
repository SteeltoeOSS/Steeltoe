// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Connector.RuntimeTypeAccess;

namespace Steeltoe.Connector.Redis.RuntimeTypeAccess;

internal sealed class ConnectionMultiplexerShim : Shim
{
    public ConnectionMultiplexerShim(StackExchangeRedisPackageResolver packageResolver, object instance)
        : base(new InstanceAccessor(packageResolver.ConnectionMultiplexerClass, instance))
    {
    }

    public static ConnectionMultiplexerShim Connect(StackExchangeRedisPackageResolver packageResolver, string configuration)
    {
        ArgumentGuard.NotNull(packageResolver);

        object instance = packageResolver.ConnectionMultiplexerClass.InvokeMethodOverload("Connect", new[]
        {
            typeof(string),
            typeof(TextWriter)
        }, null, configuration, null)!;

        return new ConnectionMultiplexerShim(packageResolver, instance);
    }
}
