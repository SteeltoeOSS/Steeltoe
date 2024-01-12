// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;

namespace Steeltoe.Discovery.Client.SimpleClients;

internal sealed class NoOpDiscoveryClient : IDiscoveryClient
{
    private readonly IList<IServiceInstance> _serviceInstances = new List<IServiceInstance>();

    public string Description => "An IDiscoveryClient that passes through to underlying infrastructure";

    internal NoOpDiscoveryClient(IConfiguration configuration, ILogger<NoOpDiscoveryClient> logger = null)
    {
        logger?.LogWarning("No discovery client has been completely configured, using default no-op discovery client.");
        logger?.LogInformation("Running in container: {IsContainerized}", Platform.IsContainerized);

        foreach (KeyValuePair<string, string> client in GetConfiguredClients(configuration))
        {
            logger?.LogWarning("Found configuration values for {client}, try adding a NuGet reference {package}", client.Key, client.Value);
        }
    }

    public Task<IList<string>> GetServicesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<string>>(new List<string>());
    }

    private Dictionary<string, string> GetConfiguredClients(IConfiguration configuration)
    {
        if (configuration is null)
        {
            throw new InvalidOperationException("IsConfigured must be called before GetConfiguredClients");
        }

        // clients (and their configuration base paths) shipped with Steeltoe
        var configurableClients = new List<Tuple<string, string, bool>>
        {
            new("Consul", "consul", true),
            new("Eureka", "eureka", true)
        };

        // allow for custom discovery client configurations to be discovered
        configurableClients.AddRange(configuration.GetSection("DiscoveryClients").GetChildren()
            .Select(x => new Tuple<string, string, bool>(x.Key, x.Value, false)));

        // iterate through the clients to see if any of them have configuration values
        var clientsWithConfig = new Dictionary<string, string>();

        foreach (Tuple<string, string, bool> potentialClient in configurableClients)
        {
            if (configuration.GetSection(potentialClient.Item2).GetChildren().Any())
            {
                clientsWithConfig.Add(potentialClient.Item1,
                    potentialClient.Item3
                        ? $"to Steeltoe.Discovery.{potentialClient.Item1}"
                        : $"that enables {potentialClient.Item1} to work with Steeltoe Discovery");
            }
        }

        return clientsWithConfig;
    }

    public Task<IList<IServiceInstance>> GetInstancesAsync(string serviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_serviceInstances);
    }

    public Task<IServiceInstance> GetLocalServiceInstanceAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("No known use case for implementing this method");
    }

    public Task ShutdownAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
