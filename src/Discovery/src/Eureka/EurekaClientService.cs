// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka.AppInfo;
using Steeltoe.Discovery.Eureka.Transport;

namespace Steeltoe.Discovery.Eureka;

public static class EurekaClientService
{
    /// <summary>
    /// Using the Eureka configuration values provided in <paramref name="configuration" /> contact the Eureka server and return all the service instances
    /// for the provided <paramref name="serviceId" />. The Eureka client is shutdown after contacting the server.
    /// </summary>
    /// <param name="configuration">
    /// configuration values used for configuring the Eureka client.
    /// </param>
    /// <param name="serviceId">
    /// the Eureka service id to look up all instances of.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// service instances.
    /// </returns>
    public static async Task<IList<IServiceInstance>> GetInstancesAsync(IConfiguration configuration, string serviceId, ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        EurekaClientOptions options = ConfigureClientOptions(configuration);
        LookupClient client = GetLookupClient(options, loggerFactory);
        IList<IServiceInstance> result = client.GetInstancesInternal(serviceId);
        await client.ShutdownAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Using the Eureka configuration values provided in <paramref name="configuration" /> contact the Eureka server and return all the registered services.
    /// The Eureka client is shutdown after contacting the server.
    /// </summary>
    /// <param name="configuration">
    /// configuration values used for configuring the Eureka client.
    /// </param>
    /// <param name="loggerFactory">
    /// Used for internal logging. Pass <see cref="NullLoggerFactory.Instance" /> to disable logging.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// all registered services.
    /// </returns>
    public static async Task<IList<string>> GetServicesAsync(IConfiguration configuration, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        EurekaClientOptions options = ConfigureClientOptions(configuration);
        LookupClient client = GetLookupClient(options, loggerFactory);
        IList<string> result = client.GetServicesInternal();
        await client.ShutdownAsync(cancellationToken);
        return result;
    }

    internal static LookupClient GetLookupClient(EurekaClientOptions options, ILoggerFactory loggerFactory)
    {
        return new LookupClient(options, null, loggerFactory);
    }

    internal static EurekaClientOptions ConfigureClientOptions(IConfiguration configuration)
    {
        IConfigurationSection clientConfigSection = configuration.GetSection(EurekaClientOptions.EurekaClientConfigurationPrefix);

        var clientOptions = new EurekaClientOptions();
        clientConfigSection.Bind(clientOptions);
        clientOptions.ShouldFetchRegistry = true;
        clientOptions.ShouldRegisterWithEureka = false;
        return clientOptions;
    }

    internal sealed class LookupClient : DiscoveryClient
    {
        public LookupClient(EurekaClientConfiguration clientConfiguration, EurekaHttpClient httpClient = null, ILoggerFactory loggerFactory = null)
            : base(clientConfiguration, httpClient, loggerFactory)
        {
            if (cacheRefreshTimer != null)
            {
                cacheRefreshTimer.Dispose();
                cacheRefreshTimer = null;
            }
        }

        public IList<IServiceInstance> GetInstancesInternal(string serviceId)
        {
            IList<InstanceInfo> infos = GetInstancesByVipAddress(serviceId, false);
            var instances = new List<IServiceInstance>();

            foreach (InstanceInfo info in infos)
            {
                logger?.LogDebug($"GetInstances returning: {info}");
                instances.Add(new EurekaServiceInstance(info));
            }

            return instances;
        }

        public IList<string> GetServicesInternal()
        {
            Applications applications = Applications;

            if (applications == null)
            {
                return new List<string>();
            }

            IList<Application> registered = applications.GetRegisteredApplications();
            var names = new List<string>();

            foreach (Application app in registered)
            {
                if (app.Instances.Count == 0)
                {
                    continue;
                }

#pragma warning disable S4040 // Strings should be normalized to uppercase
                names.Add(app.Name.ToLowerInvariant());
#pragma warning restore S4040 // Strings should be normalized to uppercase
            }

            return names;
        }
    }
}
