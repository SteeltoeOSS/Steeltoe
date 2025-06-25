// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis.DynamicTypeAccess;

internal sealed class ConnectionMultiplexerInterfaceShim(StackExchangeRedisPackageResolver packageResolver, object instance)
    : Shim(new InstanceAccessor(packageResolver.ConnectionMultiplexerInterface, instance)), IDisposable
{
    private readonly StackExchangeRedisPackageResolver _packageResolver = packageResolver;

    public override IDisposable Instance => (IDisposable)base.Instance;

    public string ClientName => InstanceAccessor.GetPropertyValue<string>("ClientName");

    public DatabaseInterfaceShim GetDatabase()
    {
        object databaseInstance = InstanceAccessor.InvokeMethod("GetDatabase", true, -1, null)!;
        return new DatabaseInterfaceShim(_packageResolver, databaseInstance);
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}
