// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Management.Tasks.Test;

public sealed class TaskHostExtensionsTest
{
    [Fact]
    public async Task WebApplication_ExecutesSingletonTask()
    {
        const string taskName = "SingletonTest";
        string[] args = [$"RunTask={taskName}"];

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        builder.Services.AddSingleton<TaskApplicationState>();
        builder.Services.AddTask<TestApplicationTask>(taskName, ServiceLifetime.Singleton);

        WebApplication app = builder.Build();

        await app.RunWithTasksAsync(CancellationToken.None);

        var sharedState = app.Services.GetRequiredService<TaskApplicationState>();
        sharedState.HasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task WebApplication_ExecutesScopedTask()
    {
        const string taskName = "ScopedTest";
        string[] args = [$"RunTask={taskName}"];

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        builder.Services.AddSingleton<TaskApplicationState>();
        builder.Services.AddTask<TestApplicationTask>(taskName);

        WebApplication app = builder.Build();

        await app.RunWithTasksAsync(CancellationToken.None);

        var sharedState = app.Services.GetRequiredService<TaskApplicationState>();
        sharedState.HasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task WebApplication_ExecutesTransientTask()
    {
        const string taskName = "TransientTest";
        string[] args = [$"RunTask={taskName}"];

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        builder.Services.AddSingleton<TaskApplicationState>();
        builder.Services.AddTask<TestApplicationTask>(taskName, ServiceLifetime.Transient);

        WebApplication app = builder.Build();

        await app.RunWithTasksAsync(CancellationToken.None);

        var sharedState = app.Services.GetRequiredService<TaskApplicationState>();
        sharedState.HasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task WebApplication_ExecutesTaskInstance()
    {
        const string taskName = "InstanceTest";
        string[] args = [$"RunTask={taskName}"];

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        var sharedState = new TaskApplicationState();
        var applicationTask = new TestApplicationTask(sharedState);

        builder.Services.AddTask(taskName, applicationTask);

        WebApplication app = builder.Build();
        await app.RunWithTasksAsync(CancellationToken.None);

        sharedState.HasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task WebApplication_ExecutesInlineTaskThatConsumesScopedService()
    {
        const string taskName = "InlineTest";
        string[] args = [$"RunTask={taskName}"];

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        bool hasExecuted = false;
        builder.Services.AddScoped<TestDependentService>();

        builder.Services.AddTask(taskName, async (serviceProvider, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            _ = serviceProvider.GetRequiredService<TestDependentService>();

            await Task.Yield();
            hasExecuted = true;
        });

        WebApplication app = builder.Build();
        await app.RunWithTasksAsync(CancellationToken.None);

        hasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task WebApplication_ExecutesTaskUsingInlineFactoryThatConsumesScopedService()
    {
        const string taskName = "FactoryTest";
        string[] args = [$"RunTask={taskName}"];

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        builder.Services.AddScoped<TestDependentService>();
        builder.Services.AddSingleton<TaskApplicationState>();

        builder.Services.AddTask(taskName, (serviceProvider, name) =>
        {
            name.Should().Be(taskName);

            _ = serviceProvider.GetRequiredService<TestDependentService>();

            var innerSharedState = serviceProvider.GetRequiredService<TaskApplicationState>();
            return new TestApplicationTask(innerSharedState);
        });

        WebApplication app = builder.Build();
        await app.RunWithTasksAsync(CancellationToken.None);

        var sharedState = app.Services.GetRequiredService<TaskApplicationState>();
        sharedState.HasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task WebApplication_LogsErrorOnUnknownTask()
    {
        string[] args = ["RunTask=DoesNotExist"];

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        var capturingLoggerProvider = new CapturingLoggerProvider(category => category.StartsWith("Steeltoe.", StringComparison.Ordinal));
        builder.Services.AddLogging(options => options.AddProvider(capturingLoggerProvider));

        WebApplication app = builder.Build();

        await app.RunWithTasksAsync(CancellationToken.None);

        IList<string> logMessages = capturingLoggerProvider.GetAll();

        logMessages.Should().BeEquivalentTo([
            "FAIL Steeltoe.Management.Tasks.CloudFoundryTasks: No task with name 'DoesNotExist' is registered in the service container."
        ]);
    }

    [Fact]
    public async Task WebApplication_PropagatesThrownException()
    {
        const string taskName = "ThrowTest";
        string[] args = [$"RunTask={taskName}"];

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        builder.Services.AddSingleton<TaskApplicationState>();
        builder.Services.AddTask<ThrowingApplicationTask>(taskName);

        WebApplication app = builder.Build();

        Func<Task> action = async () => await app.RunWithTasksAsync(CancellationToken.None);

        await action.Should().ThrowExactlyAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task WebHost_ExecutesScopedTask()
    {
        const string taskName = "ScopedTest";
        string[] args = [$"RunTask={taskName}"];

        IWebHostBuilder builder = WebHost.CreateDefaultBuilder(args);
        builder.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configure(HostingHelpers.EmptyAction);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<TaskApplicationState>();
            services.AddTask<TestApplicationTask>(taskName);
        });

        IWebHost app = builder.Build();

        await app.RunWithTasksAsync(CancellationToken.None);

        var sharedState = app.Services.GetRequiredService<TaskApplicationState>();
        sharedState.HasExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task GenericHost_ExecutesScopedTask()
    {
        const string taskName = "ScopedTest";
        string[] args = [$"RunTask={taskName}"];

        IHostBuilder builder = Host.CreateDefaultBuilder(args);
        builder.UseDefaultServiceProvider(options => options.ValidateScopes = true);

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<TaskApplicationState>();
            services.AddTask<TestApplicationTask>(taskName);
        });

        IHost app = builder.Build();

        await app.RunWithTasksAsync(CancellationToken.None);

        var sharedState = app.Services.GetRequiredService<TaskApplicationState>();
        sharedState.HasExecuted.Should().BeTrue();
    }

    private sealed class TaskApplicationState
    {
        public bool HasExecuted { get; set; }
    }

    private sealed class TestApplicationTask(TaskApplicationState sharedState) : IApplicationTask
    {
        private readonly TaskApplicationState _sharedState = sharedState;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            _sharedState.HasExecuted = true;
        }
    }

    private sealed class ThrowingApplicationTask : IApplicationTask
    {
        public Task RunAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException();
        }
    }

    private sealed class TestDependentService;
}
