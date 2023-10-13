// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Lifecycle;

namespace Steeltoe.Messaging.RabbitMQ.Hosting;

public sealed class RabbitMQHost : IHost
{
    private readonly IHost _host;

    public IServiceProvider Services => _host.Services;

    public RabbitMQHost(IHost host)
    {
        _host = host;
    }

    public static IHostBuilder CreateDefaultBuilder()
    {
        return new RabbitMQHostBuilder(Host.CreateDefaultBuilder());
    }

    public static IHostBuilder CreateDefaultBuilder(string[] args)
    {
        return new RabbitMQHostBuilder(Host.CreateDefaultBuilder(args));
    }

    public void Dispose()
    {
        _host?.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        using (IServiceScope scope = _host.Services.CreateScope())
        {
            var lifecycleProcessor = scope.ServiceProvider.GetRequiredService<ILifecycleProcessor>();
            await lifecycleProcessor.OnRefreshAsync();
        }

        await _host.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        using (IServiceScope scope = _host.Services.CreateScope())
        {
            var lifecycleProcessor = scope.ServiceProvider.GetRequiredService<ILifecycleProcessor>();
            await lifecycleProcessor.StopAsync();
        }

        await _host.StopAsync(cancellationToken);
    }
}
