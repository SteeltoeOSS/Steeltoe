// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Common;
using Steeltoe.Common.DynamicTypeAccess;

namespace Steeltoe.Connectors.Redis.DynamicTypeAccess;

internal sealed class RedisCacheOptionsShim : Shim
{
    public string InstanceName
    {
        get => InstanceAccessor.GetPropertyValue<string>("InstanceName");
        set => InstanceAccessor.SetPropertyValue("InstanceName", value);
    }

    // Property type: Func<Task<IConnectionMultiplexer>>
    public object ConnectionMultiplexerFactory
    {
        get => InstanceAccessor.GetPropertyValue<object>("ConnectionMultiplexerFactory");
        set => InstanceAccessor.SetPropertyValue("ConnectionMultiplexerFactory", value);
    }

    public RedisCacheOptionsShim(MicrosoftRedisPackageResolver packageResolver, object instance)
        : this(new InstanceAccessor(packageResolver.RedisCacheOptionsClass, instance))
    {
    }

    private RedisCacheOptionsShim(InstanceAccessor instanceAccessor)
        : base(instanceAccessor)
    {
    }

    public static RedisCacheOptionsShim CreateInstance(MicrosoftRedisPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(packageResolver);

        InstanceAccessor instanceAccessor = packageResolver.RedisCacheOptionsClass.CreateInstance(null);
        return new RedisCacheOptionsShim(instanceAccessor);
    }

    // Return type: Func<Task<IConnectionMultiplexer>>
    public object CreateTaskLambdaForConnectionMultiplexerFactory(ConnectionMultiplexerInterfaceShim connectionMultiplexer)
    {
        ArgumentGuard.NotNull(connectionMultiplexer);

        MethodInfo assignMethod = GetType().GetMethod(nameof(AssignConnectionMultiplexerFactory), BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo assignGenericMethod = assignMethod.MakeGenericMethod(connectionMultiplexer.DeclaredType);

        return assignGenericMethod.Invoke(null, [connectionMultiplexer.Instance])!;
    }

    private static Func<Task<TConnectionMultiplexer>> AssignConnectionMultiplexerFactory<TConnectionMultiplexer>(TConnectionMultiplexer connectionMultiplexer)
    {
        return () => Task.FromResult(connectionMultiplexer);
    }
}
