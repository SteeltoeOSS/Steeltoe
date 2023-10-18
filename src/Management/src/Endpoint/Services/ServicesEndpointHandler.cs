// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
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
    
    public ServicesEndpointHandler(IOptionsMonitor<ServicesEndpointOptions> options, IServiceCollection serviceCollection, ILogger<ServicesEndpointHandler> logger)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(serviceCollection);
        ArgumentGuard.NotNull(logger);

        _options = options;
        _serviceCollection = serviceCollection;
        _logger = logger;
        _lazyServiceContextDescriptor = new Lazy<ServiceContextDescriptor>(GetDescriptor);

    }

    public async Task<ServiceContextDescriptor> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        var serviceContextDescriptor = _lazyServiceContextDescriptor.Value;
        return await Task.FromResult(serviceContextDescriptor);

    }

    private ServiceContextDescriptor GetDescriptor()
    {
        ServiceContextDescriptor descriptor = new ServiceContextDescriptor();
        var applicationContext = new ApplicationContext();

        foreach (ServiceDescriptor serviceDescriptor in _serviceCollection)
        {
            var key = serviceDescriptor.ToString();

            if (!applicationContext.ContainsKey(key))
            {
                applicationContext.Add(key, new Service(serviceDescriptor));
            }
            else
            {
                _logger.LogInformation("Key already added " + key);
            }
        }

        descriptor.Add("application", applicationContext);

        return descriptor;
    }

}
