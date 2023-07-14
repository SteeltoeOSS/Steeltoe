// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis.DynamicTypeAccess;

internal sealed class ConnectionMultiplexerInterfaceShim : Shim, IDisposable
{
    private readonly StackExchangeRedisPackageResolver _packageResolver;

    public override IDisposable Instance => (IDisposable)base.Instance;

    public string ClientName => InstanceAccessor.GetPropertyValue<string>("ClientName");

    public ConnectionMultiplexerInterfaceShim(StackExchangeRedisPackageResolver packageResolver, object instance)
        : base(new InstanceAccessor(packageResolver.ConnectionMultiplexerInterface, instance))
    {
        _packageResolver = packageResolver;
    }

    public DatabaseInterfaceShim GetDatabase()
    {
        object instance = InstanceAccessor.InvokeMethod("GetDatabase", true, -1, null)!;
        return new DatabaseInterfaceShim(_packageResolver, instance);
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}
