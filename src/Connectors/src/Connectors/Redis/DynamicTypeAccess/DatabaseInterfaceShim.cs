// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis.DynamicTypeAccess;

internal sealed class DatabaseInterfaceShim(StackExchangeRedisPackageResolver packageResolver, object instance)
    : Shim(new InstanceAccessor(packageResolver.DatabaseInterface, instance))
{
    public async Task<TimeSpan> PingAsync()
    {
        InstanceAccessor runtimeInstanceAccessor = InstanceAccessor.AsRuntimeType();
        var task = (Task)runtimeInstanceAccessor.InvokeMethod("PingAsync", true, 0)!;

        await task;

        using var taskShim = new TaskShim<TimeSpan>(task);
        return taskShim.GetResult();
    }
}
