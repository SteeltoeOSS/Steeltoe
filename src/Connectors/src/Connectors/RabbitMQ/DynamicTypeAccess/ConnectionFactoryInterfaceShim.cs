// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

internal sealed class ConnectionFactoryInterfaceShim(RabbitMQPackageResolver packageResolver, object instance)
    : Shim(new InstanceAccessor(packageResolver.ConnectionFactoryInterface, instance))
{
    private readonly RabbitMQPackageResolver _packageResolver = packageResolver;

    public async Task<ConnectionInterfaceShim> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var task = (Task)InstanceAccessor.InvokeMethodOverload("CreateConnectionAsync", true, [typeof(CancellationToken)], cancellationToken)!;

        await task;

        using var taskShim = new TaskShim<IDisposable>(task);
        return new ConnectionInterfaceShim(_packageResolver, taskShim.Result);
    }
}
