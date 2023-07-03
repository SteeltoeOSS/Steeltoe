// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

internal sealed class ConnectionFactoryShim : Shim
{
    private readonly RabbitMQPackageResolver _packageResolver;

    public Uri Uri
    {
        get => InstanceAccessor.GetPropertyValue<Uri>("Uri");
        set => InstanceAccessor.SetPropertyValue("Uri", value);
    }

    private ConnectionFactoryShim(RabbitMQPackageResolver packageResolver, InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
        _packageResolver = packageResolver;
    }

    public static ConnectionFactoryShim CreateInstance(RabbitMQPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(packageResolver);

        InstanceAccessor instanceAccessor = packageResolver.ConnectionFactoryClass.CreateInstance();
        return new ConnectionFactoryShim(packageResolver, instanceAccessor);
    }

    public ConnectionInterfaceShim CreateConnection()
    {
        object instance = InstanceAccessor.InvokeMethodOverload("CreateConnection", true, Type.EmptyTypes)!;
        return new ConnectionInterfaceShim(_packageResolver, instance);
    }
}
