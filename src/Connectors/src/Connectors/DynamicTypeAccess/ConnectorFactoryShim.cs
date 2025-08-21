// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.DynamicTypeAccess;

internal sealed class ConnectorFactoryShim<TOptions> : Shim, IDisposable
    where TOptions : ConnectionStringOptions
{
    private readonly Type _connectionType;

    public override IDisposable Instance => (IDisposable)base.Instance;

    private ConnectorFactoryShim(Type connectionType, InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
        _connectionType = connectionType;
    }

    public static bool IsRegistered(Type connectionType, IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(connectionType);
        ArgumentNullException.ThrowIfNull(services);

        TypeAccessor typeAccessor = MakeGenericTypeAccessor(connectionType);
        return services.Any(descriptor => descriptor.ServiceType == typeAccessor.Type);
    }

    public static void Register(Type connectionType, IServiceCollection services, IReadOnlySet<string> serviceBindingNames,
        ConnectorCreateConnection createConnection, bool useSingletonConnection)
    {
        ArgumentNullException.ThrowIfNull(connectionType);
        ArgumentNullException.ThrowIfNull(services);

        TypeAccessor typeAccessor = MakeGenericTypeAccessor(connectionType);

        services.AddSingleton(typeAccessor.Type,
            serviceProvider => typeAccessor.CreateInstance(serviceProvider, serviceBindingNames, createConnection, useSingletonConnection).Instance);
    }

    public static ConnectorFactoryShim<TOptions> FromServiceProvider(IServiceProvider serviceProvider, Type connectionType)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(connectionType);

        TypeAccessor typeAccessor = MakeGenericTypeAccessor(connectionType);
        object instance = serviceProvider.GetRequiredService(typeAccessor.Type);

        var instanceAccessor = new InstanceAccessor(typeAccessor, instance);
        return new ConnectorFactoryShim<TOptions>(connectionType, instanceAccessor);
    }

    private static TypeAccessor MakeGenericTypeAccessor(Type connectionType)
    {
        return TypeAccessor.MakeGenericAccessor(typeof(ConnectorFactory<,>), typeof(TOptions), connectionType);
    }

    public ConnectorShim<TOptions> Get(string serviceBindingName)
    {
        object instance = InstanceAccessor.InvokeMethodOverload(nameof(ConnectorFactory<,>.Get), true, [typeof(string)], serviceBindingName)!;

        return new ConnectorShim<TOptions>(_connectionType, instance);
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}
