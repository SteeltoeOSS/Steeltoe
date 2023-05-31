// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

internal sealed class ConnectionInterfaceShim : Shim, IDisposable
{
    public override IDisposable Instance => (IDisposable)base.Instance;

    public ConnectionInterfaceShim(RabbitMQPackageResolver packageResolver, object instance)
        : base(new InstanceAccessor(packageResolver.ConnectionInterface, instance))
    {
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}
