// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
namespace Steeltoe.Management.Endpoint.Services;
internal class ServicesEndpoint : IServicesEndpoint
{
    private readonly IOptionsMonitor<ServicesEndpointOptions> _options;
    private readonly IServiceCollection _serviceCollection;
    private readonly ILogger<ServicesEndpoint> _logger;

    public IEndpointOptions Options => _options.CurrentValue;
    public ServicesEndpoint(IOptionsMonitor<ServicesEndpointOptions> options, IServiceCollection serviceCollection, ILogger<ServicesEndpoint> logger)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(serviceCollection);
        ArgumentGuard.NotNull(logger);

        _options = options;
        _serviceCollection = serviceCollection;
        _logger = logger;
    }
    public ServiceContextDescriptor Invoke()
    {
        ServiceContextDescriptor descriptor = new ServiceContextDescriptor();
        var applicationContext = new ApplicationContext();

     
        foreach (ServiceDescriptor serviceDescriptor in _serviceCollection)
        {
           
            var key = serviceDescriptor.ToString();

            if (!applicationContext.ContainsKey(key))
            {
                applicationContext.Add(key, serviceDescriptor);
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
