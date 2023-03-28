// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Connector;

internal static class ConnectionFactoryInvoker
{
    public static string GetConnectionString<TOptions>(IServiceProvider serviceProvider, string serviceBindingName, Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        Type connectionFactoryType = MakeConnectionFactoryType<TOptions>(connectionType);
        object connectionFactory = ResolveConnectionFactory(serviceProvider, connectionFactoryType);

        return InvokeGetConnectionString<TOptions>(connectionFactory, connectionFactoryType, serviceBindingName);
    }

    private static string InvokeGetConnectionString<TOptions>(object connectionFactory, Type connectionFactoryType, string serviceBindingName)
        where TOptions : ConnectionStringOptions
    {
        MethodInfo getConnectionStringMethod = connectionFactoryType.GetMethod(nameof(ConnectionFactory<TOptions, object>.GetConnectionString))!;

        return (string)getConnectionStringMethod.Invoke(connectionFactory, new object[]
        {
            serviceBindingName
        });
    }

    public static object CreateConnection<TOptions>(IServiceProvider serviceProvider, string serviceBindingName, Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        Type connectionFactoryType = MakeConnectionFactoryType<TOptions>(connectionType);
        object connectionFactory = ResolveConnectionFactory(serviceProvider, connectionFactoryType);

        return InvokeGetConnection<TOptions>(connectionFactory, connectionFactoryType, serviceBindingName);
    }

    private static object InvokeGetConnection<TOptions>(object connectionFactory, Type connectionFactoryType, string serviceBindingName)
        where TOptions : ConnectionStringOptions
    {
        MethodInfo getConnectionMethod = connectionFactoryType.GetMethod(nameof(ConnectionFactory<TOptions, object>.GetConnection))!;

        return getConnectionMethod.Invoke(connectionFactory, new object[]
        {
            serviceBindingName
        });
    }

    public static Type MakeConnectionFactoryType<TOptions>(Type connectionType)
        where TOptions : ConnectionStringOptions
    {
        return typeof(ConnectionFactory<,>).MakeGenericType(typeof(TOptions), connectionType);
    }

    private static object ResolveConnectionFactory(IServiceProvider serviceProvider, Type connectionFactoryType)
    {
        return serviceProvider.GetRequiredService(connectionFactoryType);
    }

    public static object CreateConnectionFactory(IServiceProvider serviceProvider, Type connectionFactoryType, Type connectionType)
    {
        Func<string, object> createConnection = connectionString =>
        {
            try
            {
                return Activator.CreateInstance(connectionType, connectionString);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        };

        return Activator.CreateInstance(connectionFactoryType, serviceProvider, createConnection);
    }
}
