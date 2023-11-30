// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Services;

internal class ServicesEndpointHandler : IServicesEndpointHandler
{
    private readonly IOptionsMonitor<ServicesEndpointOptions> _options;
    private readonly IServiceCollection _serviceCollection;
    private readonly ILogger<ServicesEndpointHandler> _logger;
    private readonly Lazy<ServiceContextDescriptor> _lazyServiceContextDescriptor;

    public EndpointOptions Options => _options.CurrentValue;

    public ServicesEndpointHandler(IOptionsMonitor<ServicesEndpointOptions> options, IServiceCollection serviceCollection, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(serviceCollection);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _serviceCollection = serviceCollection;
        _logger = loggerFactory.CreateLogger<ServicesEndpointHandler>();
        _lazyServiceContextDescriptor = new Lazy<ServiceContextDescriptor>(GetDescriptor);
    }

    public async Task<ServiceContextDescriptor> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        ServiceContextDescriptor serviceContextDescriptor = _lazyServiceContextDescriptor.Value;
        return await Task.FromResult(serviceContextDescriptor);
    }

    private ServiceContextDescriptor GetDescriptor()
    {
        var descriptor = new ServiceContextDescriptor();
        var applicationContext = new ApplicationContext();

        _logger.LogTrace("Fetching service container services");

        foreach (ServiceDescriptor serviceDescriptor in _serviceCollection)
        {
            applicationContext.Add(new Service(serviceDescriptor));
        }

        descriptor.Add("application", applicationContext);

        return descriptor;
    }
}
