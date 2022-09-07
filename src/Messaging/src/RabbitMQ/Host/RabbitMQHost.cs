// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Messaging.RabbitMQ.Host;

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
        return new RabbitMQHostBuilder(Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder());
    }

    public static IHostBuilder CreateDefaultBuilder(string[] args)
    {
        return new RabbitMQHostBuilder(Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args));
    }

    public static WebApplicationBuilder CreateWebApplicationBuilder(string[] args = null, Action<IConfigurationBuilder> configure = null)
    {
        return GetWebApplicationBuilder(args, configure);
    }

    private static WebApplicationBuilder GetWebApplicationBuilder(string[] args = null, Action<IConfigurationBuilder> configure = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        configure?.Invoke(builder.Configuration);
        builder.Services.ConfigureRabbitServices(builder.Configuration);
        return builder;
    }

    public void Dispose()
    {
        _host.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return _host.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _host.StopAsync(cancellationToken);
    }
}
