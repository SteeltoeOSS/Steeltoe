// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

internal sealed class ConnectionFactoryInterfaceShim(RabbitMQPackageResolver packageResolver, object instance)
    : Shim(new InstanceAccessor(packageResolver.ConnectionFactoryInterface, instance))
{
    private readonly RabbitMQPackageResolver _packageResolver = packageResolver;

    public ConnectionInterfaceShim CreateConnection(CancellationToken cancellationToken)
    {
        Task task = InvokeCreateConnectionAsync(cancellationToken);

#pragma warning disable S4462 // Calls to "async" methods should not be blocking
        // Justification: The service container needs to contain IConnection, not Task<IConnection>.
        task.Wait(cancellationToken);
#pragma warning restore S4462 // Calls to "async" methods should not be blocking

        return GetTaskResult(task);
    }

    public async Task<ConnectionInterfaceShim> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        Task task = InvokeCreateConnectionAsync(cancellationToken);

        await task;

        return GetTaskResult(task);
    }

    private Task InvokeCreateConnectionAsync(CancellationToken cancellationToken)
    {
        return (Task)InstanceAccessor.InvokeMethodOverload("CreateConnectionAsync", true, [typeof(CancellationToken)], cancellationToken)!;
    }

    private ConnectionInterfaceShim GetTaskResult(Task task)
    {
        using var taskShim = new TaskShim<IDisposable>(task);
        IDisposable connection = taskShim.GetResult();
        return new ConnectionInterfaceShim(_packageResolver, connection);
    }
}
