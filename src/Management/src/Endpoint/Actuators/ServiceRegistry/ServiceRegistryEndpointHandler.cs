// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.ServiceRegistry;

internal sealed partial class ServiceRegistryEndpointHandler : IServiceRegistryEndpointHandler
{
    private readonly IOptionsMonitor<ServiceRegistryEndpointOptions> _optionsMonitor;
    private readonly IServiceRegistry[] _serviceRegistries;
    private readonly ILogger<ServiceRegistryEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public ServiceRegistryEndpointHandler(IOptionsMonitor<ServiceRegistryEndpointOptions> optionsMonitor, IEnumerable<IDiscoveryClient> discoveryClients,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(discoveryClients);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        IDiscoveryClient[] discoveryClientArray = discoveryClients.ToArray();
        ArgumentGuard.ElementsNotNull(discoveryClientArray);

        _optionsMonitor = optionsMonitor;
        _serviceRegistries = discoveryClientArray.OfType<IServiceRegistry>().ToArray();
        _logger = loggerFactory.CreateLogger<ServiceRegistryEndpointHandler>();
    }

    public async Task<ServiceRegistryResponse> InvokeAsync(ServiceRegistryRequest argument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(argument);

        IServiceRegistry[] registries = _serviceRegistries.Where(registry => registry.IsRegistrationEnabled).ToArray();

        if (registries.Length == 0)
        {
            LogNotRegistered();
            return ServiceRegistryResponse.NotRegistered;
        }

        return argument.Status == null
            ? await GetStatusAsync(registries, cancellationToken)
            : await SetStatusAsync(registries, argument.Status, cancellationToken);
    }

    private static async Task<ServiceRegistryResponse> GetStatusAsync(IServiceRegistry[] registries, CancellationToken cancellationToken)
    {
        if (registries.Length == 1)
        {
            object status = await registries[0].GetStatusAsync(cancellationToken);
            return new ServiceRegistryResponse(status);
        }

        Dictionary<string, object> statusPerRegistry = [];

        foreach (IServiceRegistry registry in registries)
        {
            string key = registry is IDiscoveryClient discoveryClient ? discoveryClient.Description : registry.GetType().Name;
            statusPerRegistry[key] = await registry.GetStatusAsync(cancellationToken);
        }

        return new ServiceRegistryResponse(statusPerRegistry);
    }

    private async Task<ServiceRegistryResponse> SetStatusAsync(IServiceRegistry[] registries, string status, CancellationToken cancellationToken)
    {
        // Validate upfront, to prevent applying the status to only some of the registries.
        IServiceRegistry? unsupportedRegistry = Array.Find(registries, registry => !registry.IsStatusSupported(status));

        if (unsupportedRegistry != null)
        {
            LogInvalidStatus(status, unsupportedRegistry.GetType());
            return ServiceRegistryResponse.FromError($"Status '{status}' is not supported by {unsupportedRegistry.GetType().Name}.");
        }

        foreach (IServiceRegistry registry in registries)
        {
            LogChangingStatus(status, registry.GetType());
            await registry.SetStatusAsync(status, cancellationToken);
        }

        return new ServiceRegistryResponse(null);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "This app is not registered with a discovery server.")]
    private partial void LogNotRegistered();

    [LoggerMessage(Level = LogLevel.Information, Message = "Changing status to {Status} using {RegistryType}.")]
    private partial void LogChangingStatus(string status, Type registryType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Status {Status} is not supported by {RegistryType}.")]
    private partial void LogInvalidStatus(string status, Type registryType);
}
