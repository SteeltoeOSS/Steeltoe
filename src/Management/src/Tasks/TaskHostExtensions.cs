// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Management.Tasks;

public static class TaskHostExtensions
{
    /// <summary>
    /// Runs a web application and returns a <see cref="Task" /> that only completes when the cancellation token is triggered or shutdown is triggered.
    /// <para>
    /// To register your application task, use one of the extension methods in <see cref="TaskServiceCollectionExtensions" />. To execute the registered
    /// task, invoke the application with command-line argument "RunTask=taskName", where "taskName" is your task's name (case-sensitive).
    /// </para>
    /// <para>
    /// Command line arguments must be registered as a configuration source for this functionality to work.
    /// </para>
    /// </summary>
    /// <param name="host">
    /// The <see cref="IWebHost" /> to run.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to trigger shutdown.
    /// </param>
    public static async Task RunWithTasksAsync(this IWebHost host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (await FindAndRunTaskAsync(host.Services, cancellationToken))
        {
            await DisposeHostAsync(host);
        }
        else
        {
            await host.RunAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Runs an application and returns a <see cref="Task" /> that only completes when the cancellation token is triggered or shutdown is triggered.
    /// <para>
    /// To register your application task, use one of the extension methods in <see cref="TaskServiceCollectionExtensions" />. To execute the registered
    /// task, invoke the application with command-line argument "RunTask=taskName", where "taskName" is your task's name (case-sensitive).
    /// </para>
    /// <para>
    /// Command line arguments must be registered as a configuration source for this functionality to work.
    /// </para>
    /// </summary>
    /// <param name="host">
    /// The <see cref="IHost" /> to run.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to trigger shutdown.
    /// </param>
    public static async Task RunWithTasksAsync(this IHost host, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(host);

        if (await FindAndRunTaskAsync(host.Services, cancellationToken))
        {
            await DisposeHostAsync(host);
        }
        else
        {
            await host.RunAsync(cancellationToken);
        }
    }

    private static async Task<bool> FindAndRunTaskAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        string? taskName = configuration.GetValue<string?>("RunTask");

        if (taskName != null)
        {
            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            await RunTaskAsync(taskName, scope.ServiceProvider, cancellationToken);

            return true;
        }

        return false;
    }

    private static async Task RunTaskAsync(string taskName, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var task = serviceProvider.GetKeyedService<IApplicationTask>(taskName);

        if (task != null)
        {
            await task.RunAsync(cancellationToken);
        }
        else
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger($"{typeof(TaskHostExtensions).Namespace}.CloudFoundryTasks");
            logger.LogError("No task with name '{TaskName}' is registered in the service container.", taskName);
        }
    }

    private static async Task DisposeHostAsync(IDisposable host)
    {
        if (host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            host.Dispose();
        }
    }
}
