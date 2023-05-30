// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;

namespace Steeltoe.Connectors.RuntimeTypeAccess;

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

    public static void Register(Type connectionType, IServiceCollection services, IReadOnlySet<string> optionNames, ConnectorCreateConnection createConnection,
        bool useSingletonConnection)
    {
        ArgumentGuard.NotNull(connectionType);
        ArgumentGuard.NotNull(services);

        TypeAccessor typeAccessor = MakeGenericTypeAccessor(connectionType);

        services.AddSingleton(typeAccessor.Type,
            serviceProvider => typeAccessor.CreateInstance(serviceProvider, optionNames, createConnection, useSingletonConnection).Instance);
    }

    public static ConnectorFactoryShim<TOptions> FromServiceProvider(IServiceProvider serviceProvider, Type connectionType)
    {
        ArgumentGuard.NotNull(serviceProvider);
        ArgumentGuard.NotNull(connectionType);

        TypeAccessor typeAccessor = MakeGenericTypeAccessor(connectionType);
        object instance = serviceProvider.GetRequiredService(typeAccessor.Type);

        var instanceAccessor = new InstanceAccessor(typeAccessor, instance);
        return new ConnectorFactoryShim<TOptions>(connectionType, instanceAccessor);
    }

    private static TypeAccessor MakeGenericTypeAccessor(Type connectionType)
    {
        return TypeAccessor.MakeGenericAccessor(typeof(ConnectorFactory<,>), typeof(TOptions), connectionType);
    }

    public ConnectorShim<TOptions> GetNamed(string name)
    {
        object instance = InstanceAccessor.InvokeMethod(nameof(ConnectorFactory<TOptions, object>.GetNamed), true, name)!;
        return new ConnectorShim<TOptions>(_connectionType, instance);
    }

    public void Dispose()
    {
        Instance.Dispose();
    }
}
