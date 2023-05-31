// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis.DynamicTypeAccess;

internal sealed class ConnectionMultiplexerShim : Shim, IDisposable
{
    public override IDisposable Instance => (IDisposable)base.Instance;

    public ConnectionMultiplexerShim(StackExchangeRedisPackageResolver packageResolver, object instance)
        : base(new InstanceAccessor(packageResolver.ConnectionMultiplexerClass, instance))
    {
    }

    public static ConnectionMultiplexerShim Connect(StackExchangeRedisPackageResolver packageResolver, string configuration)
    {
        ArgumentGuard.NotNull(packageResolver);

        object instance = packageResolver.ConnectionMultiplexerClass.InvokeMethodOverload("Connect", true, new[]
        {
            typeof(string),
            typeof(TextWriter)
        }, configuration, null)!;

        return new ConnectionMultiplexerShim(packageResolver, instance);
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}
