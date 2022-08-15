// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Get configuration info for all services of a given service type.
    /// </summary>
    /// <typeparam name="TServiceInfo">
    /// Service info type you're looking for.
    /// </typeparam>
    /// <param name="configuration">
    /// Configuration to search.
    /// </param>
    /// <returns>
    /// List of service infos.
    /// </returns>
    public static IEnumerable<TServiceInfo> GetServiceInfos<TServiceInfo>(this IConfiguration configuration)
        where TServiceInfo : class
    {
        return ServiceInfoCreatorFactory.GetServiceInfoCreator(configuration).GetServiceInfos<TServiceInfo>();
    }

    /// <summary>
    /// Get configuration info for all services of a given service type.
    /// </summary>
    /// <param name="configuration">
    /// Configuration to search.
    /// </param>
    /// <param name="infoType">
    /// Type to search for.
    /// </param>
    /// <returns>
    /// A list of relevant <see cref="IServiceInfo" />.
    /// </returns>
    public static IEnumerable<IServiceInfo> GetServiceInfos(this IConfiguration configuration, Type infoType)
    {
        return ServiceInfoCreatorFactory.GetServiceInfoCreator(configuration).GetServiceInfos(infoType);
    }

    /// <summary>
    /// Get service info when you know the ID.
    /// </summary>
    /// <param name="configuration">
    /// Configuration to search.
    /// </param>
    /// <param name="id">
    /// Id of service.
    /// </param>
    /// <returns>
    /// Requested implementation of <see cref="IServiceInfo" />.
    /// </returns>
    public static IServiceInfo GetServiceInfo(this IConfiguration configuration, string id)
    {
        return ServiceInfoCreatorFactory.GetServiceInfoCreator(configuration).GetServiceInfo(id);
    }

    /// <summary>
    /// Get service info of a given type when you know the ID.
    /// </summary>
    /// <typeparam name="TServiceInfo">
    /// Service info type you're looking for.
    /// </typeparam>
    /// <param name="configuration">
    /// Configuration to search.
    /// </param>
    /// <param name="id">
    /// Id of service.
    /// </param>
    /// <returns>
    /// Requested implementation of <see cref="IServiceInfo" />.
    /// </returns>
    public static TServiceInfo GetServiceInfo<TServiceInfo>(this IConfiguration configuration, string id)
        where TServiceInfo : class
    {
        return ServiceInfoCreatorFactory.GetServiceInfoCreator(configuration).GetServiceInfo<TServiceInfo>(id);
    }

    /// <summary>
    /// Get Service Info from IConfiguration.
    /// </summary>
    /// <typeparam name="TServiceInfo">
    /// Type of Service Info to return.
    /// </typeparam>
    /// <param name="config">
    /// Configuration to retrieve service info from.
    /// </param>
    /// <exception cref="ConnectorException">
    /// Thrown when multiple matching services are found.
    /// </exception>
    /// <returns>
    /// Information required to connect to provisioned service.
    /// </returns>
    public static TServiceInfo GetSingletonServiceInfo<TServiceInfo>(this IConfiguration config)
        where TServiceInfo : class
    {
        IEnumerable<TServiceInfo> results = GetServiceInfos<TServiceInfo>(config);

        if (results.Any())
        {
            if (results.Count() != 1)
            {
                throw new ConnectorException($"Multiple services of type: {typeof(TServiceInfo)}, bound to application.");
            }

            return results.First();
        }

        return null;
    }

    /// <summary>
    /// Get info for a named service.
    /// </summary>
    /// <typeparam name="TServiceInfo">
    /// Type of Service Info to return.
    /// </typeparam>
    /// <param name="configuration">
    /// Configuration to retrieve service info from.
    /// </param>
    /// <param name="serviceName">
    /// Name of the service.
    /// </param>
    /// <exception cref="ConnectorException">
    /// Thrown when service info isn't found.
    /// </exception>
    /// <returns>
    /// Information required to connect to provisioned service.
    /// </returns>
    public static TServiceInfo GetRequiredServiceInfo<TServiceInfo>(this IConfiguration configuration, string serviceName)
        where TServiceInfo : class
    {
        var serviceInfo = GetServiceInfo<TServiceInfo>(configuration, serviceName);

        if (serviceInfo == null)
        {
            throw new ConnectorException($"No service with name: {serviceName} found.");
        }

        return serviceInfo;
    }

    /// <summary>
    /// Evaluate whether an IConfiguration contains services bound by Cloud Foundry.
    /// </summary>
    /// <param name="configuration">
    /// Application Configuration.
    /// </param>
    /// <returns>
    /// true if vcap:services found in config, otherwise false.
    /// </returns>
    public static bool HasCloudFoundryServiceConfigurations(this IConfiguration configuration)
    {
        return configuration.GetSection("vcap:services").GetChildren().Any();
    }

    /// <summary>
    /// Adds a configuration provider that uses Connector logic to fulfill requests for GetConnectionString("serviceType") or
    /// GetConnectionString("serviceBindingName").
    /// </summary>
    /// <param name="builder">
    /// <see cref="IConfigurationBuilder" />.
    /// </param>
    /// <returns>
    /// <see cref="IConfigurationBuilder" /> with <see cref="ConnectionStringConfigurationSource" /> added.
    /// </returns>
    public static IConfigurationBuilder AddConnectionStrings(this IConfigurationBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Add(new ConnectionStringConfigurationSource(builder.Sources));
        return builder;
    }
}
