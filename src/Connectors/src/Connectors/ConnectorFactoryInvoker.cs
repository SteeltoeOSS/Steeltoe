// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Connectors;

internal static class ConnectorFactoryInvoker
{
    public static string GetConnectionString<TOptions>(IServiceProvider serviceProvider, string serviceBindingName, Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        Type connectorFactoryType = MakeConnectorFactoryType<TOptions>(connectionType);
        object connectorFactory = ResolveConnectorFactory(serviceProvider, connectorFactoryType);

        Type connectorType = MakeConnectorType<TOptions>(connectionType);
        object connector = InvokeConnectorFactoryGetNamed<TOptions>(connectorFactory, connectorFactoryType, serviceBindingName);

        return InvokeGetConnectionString<TOptions>(connector, connectorType);
    }

    public static object GetConnection<TOptions>(IServiceProvider serviceProvider, string serviceBindingName, Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        Type connectorFactoryType = MakeConnectorFactoryType<TOptions>(connectionType);
        object connectorFactory = ResolveConnectorFactory(serviceProvider, connectorFactoryType);

        Type connectorType = MakeConnectorType<TOptions>(connectionType);
        object connector = InvokeConnectorFactoryGetNamed<TOptions>(connectorFactory, connectorFactoryType, serviceBindingName);

        return InvokeGetConnection<TOptions>(connector, connectorType);
    }

    public static Type MakeConnectorFactoryType<TOptions>(Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        return typeof(ConnectorFactory<,>).MakeGenericType(typeof(TOptions), connectionType);
    }

    private static Type MakeConnectorType<TOptions>(Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        return typeof(Connector<,>).MakeGenericType(typeof(TOptions), connectionType);
    }

    private static object InvokeConnectorFactoryGetNamed<TOptions>(object connectorFactory, Type connectorFactoryType, string serviceBindingName)
        where TOptions : ConnectionStringOptions
    {
        MethodInfo getNamedMethod = connectorFactoryType.GetMethod(nameof(ConnectorFactory<TOptions, object>.GetNamed))!;

        return getNamedMethod.Invoke(connectorFactory, new object[]
        {
            serviceBindingName
        });
    }

    private static string InvokeGetConnectionString<TOptions>(object connector, Type connectorType)
        where TOptions : ConnectionStringOptions
    {
        PropertyInfo optionsProperty = connectorType.GetProperty(nameof(Connector<TOptions, object>.Options))!;
        object options = optionsProperty.GetMethod!.Invoke(connector, null);

        PropertyInfo connectionStringProperty = typeof(TOptions).GetProperty(nameof(ConnectionStringOptions.ConnectionString))!;
        return (string)connectionStringProperty.GetMethod!.Invoke(options, null);
    }

    private static object InvokeGetConnection<TOptions>(object connector, Type connectorType)
        where TOptions : ConnectionStringOptions
    {
        MethodInfo createConnectionMethod = connectorType.GetMethod(nameof(Connector<TOptions, object>.GetConnection))!;

        return createConnectionMethod.Invoke(connector, null);
    }

    private static object ResolveConnectorFactory(IServiceProvider serviceProvider, Type connectorFactoryType)
    {
        return serviceProvider.GetRequiredService(connectorFactoryType);
    }

    public static object CreateConnectorFactory<TOptions>(IServiceProvider serviceProvider, Type connectorFactoryType, bool useSingletonConnection,
        Func<TOptions, string, object> createConnection)
        where TOptions : ConnectionStringOptions
    {
        return Activator.CreateInstance(connectorFactoryType, serviceProvider, createConnection, useSingletonConnection);
    }
}
