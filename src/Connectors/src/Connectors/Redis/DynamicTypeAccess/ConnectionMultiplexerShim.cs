// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

    public static ConnectionMultiplexerInterfaceShim Connect(StackExchangeRedisPackageResolver packageResolver, string? configuration)
    {
        ArgumentGuard.NotNull(packageResolver);

        object instance = packageResolver.ConnectionMultiplexerClass.InvokeMethodOverload("Connect", true, new[]
        {
            typeof(string),
            typeof(TextWriter)
        }, configuration, null)!;

        return new ConnectionMultiplexerInterfaceShim(packageResolver, instance);
    }

    public static async Task<ConnectionMultiplexerInterfaceShim> ConnectAsync(StackExchangeRedisPackageResolver packageResolver, string? configuration)
    {
        ArgumentGuard.NotNull(packageResolver);

        var task = (Task)packageResolver.ConnectionMultiplexerClass.InvokeMethodOverload("ConnectAsync", true, new[]
        {
            typeof(string),
            typeof(TextWriter)
        }, configuration, null)!;

        await task;

        using var taskShim = new TaskShim<IDisposable>(task);
        return new ConnectionMultiplexerInterfaceShim(packageResolver, taskShim.Result);
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}
