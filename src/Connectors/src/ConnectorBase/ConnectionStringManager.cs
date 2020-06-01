// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.CloudFoundry.Connector
{
    public class ConnectionStringManager
    {
        private readonly IConfiguration _configuration;

        public ConnectionStringManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Connection Get<T>(string serviceName = null)
            where T : IConnectionInfo, new()
        {
            return new T().Get(_configuration, serviceName);
        }
    }
}