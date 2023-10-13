// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connectors.Services;

namespace Steeltoe.Connectors.CloudFoundry;

internal static class ConfigurationExtensions
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
        return ServiceInfoCreatorFactory.GetServiceInfoCreator(configuration).GetServiceInfosOfType<TServiceInfo>();
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
        return ServiceInfoCreatorFactory.GetServiceInfoCreator(configuration).GetServiceInfosOfType(infoType);
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
        where TServiceInfo : class, IServiceInfo
    {
        return ServiceInfoCreatorFactory.GetServiceInfoCreator(configuration).GetServiceInfo<TServiceInfo>(id);
    }

    /// <summary>
    /// Get Service Info from IConfiguration.
    /// </summary>
    /// <typeparam name="TServiceInfo">
    /// Type of Service Info to return.
    /// </typeparam>
    /// <param name="configuration">
    /// Configuration to retrieve service info from.
    /// </param>
    /// <exception cref="ConnectorException">
    /// Thrown when multiple matching services are found.
    /// </exception>
    /// <returns>
    /// Information required to connect to provisioned service.
    /// </returns>
    public static TServiceInfo GetSingletonServiceInfo<TServiceInfo>(this IConfiguration configuration)
        where TServiceInfo : class
    {
        TServiceInfo[] results = GetServiceInfos<TServiceInfo>(configuration).ToArray();

        if (results.Length > 0)
        {
            if (results.Length != 1)
            {
                throw new ConnectorException($"Multiple services of type: {typeof(TServiceInfo)}, bound to application.");
            }

            return results[0];
        }

        return null;
    }
}
