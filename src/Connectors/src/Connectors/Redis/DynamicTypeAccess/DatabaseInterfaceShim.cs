// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis.DynamicTypeAccess;

internal sealed class DatabaseInterfaceShim : Shim
{
    public DatabaseInterfaceShim(StackExchangeRedisPackageResolver packageResolver, object instance)
        : base(new InstanceAccessor(packageResolver.DatabaseInterface, instance))
    {
    }

    public TimeSpan Ping()
    {
        InstanceAccessor runtimeInstanceAccessor = InstanceAccessor.AsRuntimeType();
        return (TimeSpan)runtimeInstanceAccessor.InvokeMethod("Ping", true, 0)!;
    }
}
