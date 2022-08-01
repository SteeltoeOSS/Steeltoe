// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Stream.Binding;

namespace Steeltoe.Stream.StreamHost;

public sealed class StreamHost : IHost
{
    public static IHostBuilder CreateDefaultBuilder<T>() => new StreamsHostBuilder<T>(Host.CreateDefaultBuilder());

    public static IHostBuilder CreateDefaultBuilder<T>(string[] args) => new StreamsHostBuilder<T>(Host.CreateDefaultBuilder(args));

    public StreamHost(IHost host)
    {
        _host = host;
    }

    public IServiceProvider Services => _host.Services;

    private readonly IHost _host;

    public void Dispose()
    {
        _host?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        var lifecycleProcessor = _host.Services.GetRequiredService<ILifecycleProcessor>();
        lifecycleProcessor.OnRefresh();
        var processor = _host.Services.GetRequiredService<StreamListenerAttributeProcessor>();
        processor.Initialize();
        return _host.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        var lifecycleProcessor = _host.Services.GetRequiredService<ILifecycleProcessor>();
        lifecycleProcessor.Stop();

        // Stop that thing
        return _host.StopAsync(cancellationToken);
    }
}