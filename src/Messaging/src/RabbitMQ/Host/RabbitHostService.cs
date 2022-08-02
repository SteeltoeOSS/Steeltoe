// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Core;

namespace Steeltoe.Messaging.RabbitMQ.Host;

public class RabbitHostService : IHostedService
{
    private readonly IApplicationContext _applicationContext;
    private readonly ILogger _logger;

    public RabbitHostService(IApplicationContext applicationContext, ILogger<RabbitHostService> logger = null)
    {
        _applicationContext = applicationContext;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Starting RabbitHostService ...");

        // Ensure any admins in the container get initialized
        IEnumerable<IRabbitAdmin> admins = _applicationContext.GetServices<IRabbitAdmin>();

        if (admins == null || !admins.Any())
        {
            _logger?.LogInformation("Found no IRabbitAdmin services to initialize");
        }

        // Allow for processing of RabbitListenerAttributes
        var listenerProcessor = _applicationContext.GetService<IRabbitListenerAttributeProcessor>();

        if (listenerProcessor == null)
        {
            _logger?.LogInformation("Found no IRabbitListenerAttributeProcessor services to initialize");
        }
        else
        {
            listenerProcessor.Initialize();
        }

        // Ensure any RabbitContainers get started
        var processor = _applicationContext.GetService<ILifecycleProcessor>();

        if (processor != null)
        {
            await processor.OnRefresh();
        }
        else
        {
            _logger?.LogInformation("Found no ILifecycleProcessor service to initialize");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Stopping RabbitHostService ...");
        return Task.CompletedTask;
    }
}
