// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.DynamicTypeAccess;

internal sealed class ConnectorShim<TOptions>(Type connectionType, object instance) : Shim(CreateAccessor(connectionType, instance)), IDisposable
    where TOptions : ConnectionStringOptions
{
    public override IDisposable Instance => (IDisposable)base.Instance;

    public TOptions Options => InstanceAccessor.GetPropertyValue<TOptions>("Options");

    private static InstanceAccessor CreateAccessor(Type connectionType, object instance)
    {
        ArgumentNullException.ThrowIfNull(connectionType);
        ArgumentNullException.ThrowIfNull(instance);

        var typeAccessor = TypeAccessor.MakeGenericAccessor(typeof(Connector<,>), typeof(TOptions), connectionType);
        return new InstanceAccessor(typeAccessor, instance);
    }

    public object GetConnection()
    {
        return InstanceAccessor.InvokeMethod(nameof(Connector<TOptions, object>.GetConnection), true)!;
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}
