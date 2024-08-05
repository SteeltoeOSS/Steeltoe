// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;

namespace Steeltoe.Management.Tasks;

/// <summary>
/// Provides extension methods to register an <see cref="IApplicationTask" /> in the service container.
/// </summary>
public static class TaskServiceCollectionExtensions
{
    /// <summary>
    /// Registers an application task that can be executed from the command line.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="taskName">
    /// The case-sensitive name of the task.
    /// </param>
    /// <typeparam name="T">
    /// The type of the task to execute.
    /// </typeparam>
    public static void AddTask<T>(this IServiceCollection services, string taskName)
        where T : class, IApplicationTask
    {
        AddTask<T>(services, taskName, ServiceLifetime.Scoped);
    }

    /// <summary>
    /// Registers an application task that can be executed from the command line.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="taskName">
    /// The case-sensitive name of the task.
    /// </param>
    /// <param name="lifetime">
    /// The <see cref="ServiceLifetime" /> of the task.
    /// </param>
    /// <typeparam name="T">
    /// The type of the task to execute.
    /// </typeparam>
    public static void AddTask<T>(this IServiceCollection services, string taskName, ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);

        services.TryAdd(new ServiceDescriptor(typeof(IApplicationTask), taskName, typeof(T), lifetime));
    }

    /// <summary>
    /// Registers an application task instance that can be executed from the command line.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="taskName">
    /// The case-sensitive name of the task.
    /// </param>
    /// <param name="task">
    /// The task instance to execute.
    /// </param>
    public static void AddTask(this IServiceCollection services, string taskName, IApplicationTask task)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);
        ArgumentNullException.ThrowIfNull(task);

        services.TryAddKeyedSingleton(taskName, task);
    }

    /// <summary>
    /// Registers an inline application task that can be executed from the command line.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="taskName">
    /// The case-sensitive name of the task.
    /// </param>
    /// <param name="asyncAction">
    /// The asynchronous action to execute.
    /// </param>
    public static void AddTask(this IServiceCollection services, string taskName, Func<IServiceProvider, CancellationToken, Task> asyncAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);
        ArgumentNullException.ThrowIfNull(asyncAction);

        services.TryAddKeyedScoped<IApplicationTask>(taskName,
            (serviceProvider, _) => new DelegatingTask(cancellationToken => asyncAction(serviceProvider, cancellationToken)));
    }

    /// <summary>
    /// Registers a factory for an application task that can be executed from the command line.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="taskName">
    /// The case-sensitive name of the task.
    /// </param>
    /// <param name="factory">
    /// The factory method that creates an <see cref="IApplicationTask" /> instance from an <see cref="IServiceProvider" /> and task name.
    /// </param>
    public static void AddTask(this IServiceCollection services, string taskName, Func<IServiceProvider, string, IApplicationTask> factory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);
        ArgumentNullException.ThrowIfNull(factory);

        services.TryAddKeyedScoped(taskName, (serviceProvider, _) => factory(serviceProvider, taskName));
    }
}
