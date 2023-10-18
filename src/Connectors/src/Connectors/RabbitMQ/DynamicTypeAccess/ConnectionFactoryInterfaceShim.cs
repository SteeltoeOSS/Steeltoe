// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

internal sealed class ConnectionFactoryInterfaceShim : Shim
{
    private readonly RabbitMQPackageResolver _packageResolver;

    public ConnectionFactoryInterfaceShim(RabbitMQPackageResolver packageResolver, object instance)
        : base(new InstanceAccessor(packageResolver.ConnectionFactoryInterface, instance))
    {
        _packageResolver = packageResolver;
    }

    public ConnectionInterfaceShim CreateConnection()
    {
        object instance = InstanceAccessor.InvokeMethodOverload("CreateConnection", true, Type.EmptyTypes)!;
        return new ConnectionInterfaceShim(_packageResolver, instance);
    }
}
