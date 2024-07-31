// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;

namespace Steeltoe.Management.Tasks;

public static class TaskServiceCollectionExtensions
{
    /// <summary>
    /// Register a one-off task that can be executed from command line.
    /// </summary>
    /// <param name="services">
    /// Service container.
    /// </param>
    /// <param name="lifetime">
    /// Task lifetime.
    /// </param>
    /// <typeparam name="T">
    /// Task implementation.
    /// </typeparam>
    public static void AddTask<T>(this IServiceCollection services, ServiceLifetime lifetime)
        where T : class, IApplicationTask
    {
        ArgumentGuard.NotNull(services);

        services.Add(new ServiceDescriptor(typeof(IApplicationTask), typeof(T), lifetime));
    }

    /// <summary>
    /// Register a one-off task that can be executed from command line.
    /// </summary>
    /// <param name="services">
    /// Service container.
    /// </param>
    /// <typeparam name="T">
    /// Task implementation.
    /// </typeparam>
    public static void AddTask<T>(this IServiceCollection services)
        where T : class, IApplicationTask
    {
        services.AddTask<T>(ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Register a one-off task that can be executed from command line.
    /// </summary>
    /// <param name="services">
    /// Service container.
    /// </param>
    /// <param name="task">
    /// Task instance.
    /// </param>
    public static void AddTask(this IServiceCollection services, IApplicationTask task)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(task);

        services.Add(new ServiceDescriptor(typeof(IApplicationTask), task));
    }

    /// <summary>
    /// Register a one-off task that can be executed from command line.
    /// </summary>
    /// <param name="services">
    /// Service container.
    /// </param>
    /// <param name="factory">
    /// A factory method to create an application task.
    /// </param>
    /// <param name="lifetime">
    /// Task lifetime.
    /// </param>
    public static void AddTask(this IServiceCollection services, Func<IServiceProvider, IApplicationTask> factory, ServiceLifetime lifetime)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(factory);

        services.Add(new ServiceDescriptor(typeof(IApplicationTask), factory, lifetime));
    }

    /// <summary>
    /// Register a one-off task that can be executed from command line.
    /// </summary>
    /// <param name="services">
    /// Service container.
    /// </param>
    /// <param name="factory">
    /// A factory method to create an application task.
    /// </param>
    public static void AddTask(this IServiceCollection services, Func<IServiceProvider, IApplicationTask> factory)
    {
        services.AddTask(factory, ServiceLifetime.Singleton);
    }

    /// <summary>
    /// Register a one-off task that can be executed from command line.
    /// </summary>
    /// <param name="services">
    /// Service container.
    /// </param>
    /// <param name="name">
    /// Well known name of the task. This is how it's identified when called.
    /// </param>
    /// <param name="runAction">
    /// Task method body.
    /// </param>
    /// <param name="lifetime">
    /// Task lifetime.
    /// </param>
    public static void AddTask(this IServiceCollection services, string name, Action<IServiceProvider> runAction, ServiceLifetime lifetime)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(name);
        ArgumentGuard.NotNull(runAction);

        services.Add(new ServiceDescriptor(typeof(IApplicationTask), svc => new DelegatingTask(name, () => runAction(svc)), lifetime));
    }

    /// <summary>
    /// Register a one-off task that can be executed from command line.
    /// </summary>
    /// <param name="services">
    /// Service container.
    /// </param>
    /// <param name="name">
    /// Well known name of the task. This is how it's identified when called.
    /// </param>
    /// <param name="runAction">
    /// Task method body.
    /// </param>
    public static void AddTask(this IServiceCollection services, string name, Action<IServiceProvider> runAction)
    {
        services.AddTask(name, runAction, ServiceLifetime.Singleton);
    }
}
