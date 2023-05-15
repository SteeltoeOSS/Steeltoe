// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Connectors;

internal static class ConnectionFactoryInvoker
{
    public static string GetConnectionString<TOptions>(IServiceProvider serviceProvider, string serviceBindingName, Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        Type connectionFactoryType = MakeConnectionFactoryType<TOptions>(connectionType);
        object connectionFactory = ResolveConnectionFactory(serviceProvider, connectionFactoryType);

        Type connectionProviderType = MakeConnectionProviderType<TOptions>(connectionType);
        object connectionProvider = InvokeConnectionFactoryGetNamed<TOptions>(connectionFactory, connectionFactoryType, serviceBindingName);

        return InvokeGetConnectionString<TOptions>(connectionProvider, connectionProviderType);
    }

    public static object GetConnection<TOptions>(IServiceProvider serviceProvider, string serviceBindingName, Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        Type connectionFactoryType = MakeConnectionFactoryType<TOptions>(connectionType);
        object connectionFactory = ResolveConnectionFactory(serviceProvider, connectionFactoryType);

        Type connectionProviderType = MakeConnectionProviderType<TOptions>(connectionType);
        object connectionProvider = InvokeConnectionFactoryGetNamed<TOptions>(connectionFactory, connectionFactoryType, serviceBindingName);

        return InvokeGetConnection<TOptions>(connectionProvider, connectionProviderType);
    }

    public static Type MakeConnectionFactoryType<TOptions>(Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        return typeof(ConnectionFactory<,>).MakeGenericType(typeof(TOptions), connectionType);
    }

    private static Type MakeConnectionProviderType<TOptions>(Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        return typeof(ConnectionProvider<,>).MakeGenericType(typeof(TOptions), connectionType);
    }

    private static object InvokeConnectionFactoryGetNamed<TOptions>(object connectionFactory, Type connectionFactoryType, string serviceBindingName)
        where TOptions : ConnectionStringOptions
    {
        MethodInfo getNamedMethod = connectionFactoryType.GetMethod(nameof(ConnectionFactory<TOptions, object>.GetNamed))!;

        return getNamedMethod.Invoke(connectionFactory, new object[]
        {
            serviceBindingName
        });
    }

    private static string InvokeGetConnectionString<TOptions>(object connectionProvider, Type connectionProviderType)
        where TOptions : ConnectionStringOptions
    {
        PropertyInfo optionsProperty = connectionProviderType.GetProperty(nameof(ConnectionProvider<TOptions, object>.Options))!;
        object options = optionsProperty.GetMethod!.Invoke(connectionProvider, null);

        PropertyInfo connectionStringProperty = typeof(TOptions).GetProperty(nameof(ConnectionStringOptions.ConnectionString))!;
        return (string)connectionStringProperty.GetMethod!.Invoke(options, null);
    }

    private static object InvokeGetConnection<TOptions>(object connectionProvider, Type connectionProviderType)
        where TOptions : ConnectionStringOptions
    {
        MethodInfo createConnectionMethod = connectionProviderType.GetMethod(nameof(ConnectionProvider<TOptions, object>.GetConnection))!;

        return createConnectionMethod.Invoke(connectionProvider, null);
    }

    private static object ResolveConnectionFactory(IServiceProvider serviceProvider, Type connectionFactoryType)
    {
        return serviceProvider.GetRequiredService(connectionFactoryType);
    }

    public static object CreateConnectionFactory<TOptions>(IServiceProvider serviceProvider, Type connectionFactoryType, bool useSingletonConnection,
        Func<TOptions, string, object> createConnection)
        where TOptions : ConnectionStringOptions
    {
        return Activator.CreateInstance(connectionFactoryType, serviceProvider, createConnection, useSingletonConnection);
    }
}
