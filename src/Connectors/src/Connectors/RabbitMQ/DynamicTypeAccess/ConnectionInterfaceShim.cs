// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

internal sealed class ConnectionInterfaceShim(RabbitMQPackageResolver packageResolver, object instance)
    : Shim(new InstanceAccessor(packageResolver.ConnectionInterface, instance)), IDisposable
{
    public override IDisposable Instance => (IDisposable)base.Instance;

    public bool IsOpen => InstanceAccessor.GetPropertyValue<bool>("IsOpen");

    public IDictionary<string, object> ServerProperties => InstanceAccessor.GetPropertyValue<IDictionary<string, object>>("ServerProperties");

    public void Dispose()
    {
        Instance.Dispose();
    }
}
