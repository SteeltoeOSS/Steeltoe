// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Steeltoe.CloudFoundry.Connector
{
    public interface IConnectionInfo
    {
        Connection Get(IConfiguration configuration, string serviceName);
    }

    public class Connection
    {
        public string ConnectionString { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();
    }
}