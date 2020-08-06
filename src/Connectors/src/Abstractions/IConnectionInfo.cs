// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Connector.Services;
using System.Collections.Generic;

namespace Steeltoe.Connector
{
    public interface IConnectionInfo
    {
        /// <summary>
        /// Determines if this <see cref="IConnectionInfo"/> is compatible with a service type name
        /// </summary>
        /// <param name="serviceType">The name of a service type to match</param>
        /// <returns>True when this type is compatible</returns>
        bool IsSameType(string serviceType);

        /// <summary>
        /// Determines if this <see cref="IConnectionInfo"/> is compatible with a service info
        /// </summary>
        /// <param name="serviceInfo">The service info to match</param>
        /// <returns>True when this type is compatible</returns>
        bool IsSameType(IServiceInfo serviceInfo);

        /// <summary>
        /// Get connection information from configuration for a named service
        /// </summary>
        /// <param name="configuration"><see cref="IConfiguration"/></param>
        /// <param name="serviceName">The name of the service to retrieve</param>
        /// <returns>Connection information</returns>
        Connection Get(IConfiguration configuration, string serviceName);

        /// <summary>
        /// Get connection information from configuration after a service info has been discovered
        /// </summary>
        /// <param name="configuration"><see cref="IConfiguration"/></param>
        /// <param name="serviceInfo"><see cref="IServiceInfo"/></param>
        /// <returns>Connection information</returns>
        Connection Get(IConfiguration configuration, IServiceInfo serviceInfo);
    }

    public class Connection
    {
        public Connection(string connectionString, string serviceType, IServiceInfo serviceInfo)
        {
            ConnectionString = connectionString;
            Name = serviceType + serviceInfo?.Id?.Insert(0, "-");
        }

        /// <summary>
        /// Gets or sets the connection string for this connection
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the name of this connection
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets a list of additional properties for this connection
        /// </summary>
        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();
    }
}