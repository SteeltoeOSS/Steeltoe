// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Hosting;

namespace Steeltoe.Common.Logging;

internal sealed class BootstrapLoggerHostedService : IHostedService
{
    private readonly BootstrapLoggerContext _context;
    private readonly ILoggerFactory _loggerFactory;

    public BootstrapLoggerHostedService(BootstrapLoggerContext context, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _context = context;
        _loggerFactory = loggerFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        IBootstrapLoggerFactory bootstrapLoggerFactory = _context.GetBootstrapLoggerFactory();
        bootstrapLoggerFactory.Update(_loggerFactory);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public static void Register(HostBuilderWrapper wrapper)
    {
        ArgumentNullException.ThrowIfNull(wrapper);

        wrapper.ConfigureServices(services =>
        {
            services.TryAddSingleton(_ => new BootstrapLoggerContext(wrapper));
            services.AddHostedService<BootstrapLoggerHostedService>();
        });
    }
}
