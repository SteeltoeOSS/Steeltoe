// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis.DynamicTypeAccess;

internal sealed class ConnectionMultiplexerShim(StackExchangeRedisPackageResolver packageResolver, object instance)
    : Shim(new InstanceAccessor(packageResolver.ConnectionMultiplexerClass, instance)), IDisposable
{
    public override IDisposable Instance => (IDisposable)base.Instance;

    public static ConnectionMultiplexerInterfaceShim Connect(StackExchangeRedisPackageResolver packageResolver, string? configuration)
    {
        ArgumentNullException.ThrowIfNull(packageResolver);

        object instance = packageResolver.ConnectionMultiplexerClass.InvokeMethodOverload("Connect", true, [
            typeof(string),
            typeof(TextWriter)
        ], configuration, null)!;

        return new ConnectionMultiplexerInterfaceShim(packageResolver, instance);
    }

    public static async Task<ConnectionMultiplexerInterfaceShim> ConnectAsync(StackExchangeRedisPackageResolver packageResolver, string? configuration)
    {
        ArgumentNullException.ThrowIfNull(packageResolver);

        var task = (Task)packageResolver.ConnectionMultiplexerClass.InvokeMethodOverload("ConnectAsync", true, [
            typeof(string),
            typeof(TextWriter)
        ], configuration, null)!;

        await task;

        using var taskShim = new TaskShim<IDisposable>(task);
        IDisposable connectionMultiplexer = taskShim.GetResult();
        return new ConnectionMultiplexerInterfaceShim(packageResolver, connectionMultiplexer);
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}
