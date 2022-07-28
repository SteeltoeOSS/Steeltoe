// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using System.Collections.Generic;

namespace Steeltoe.Connector;

public class Connection
{
    public Connection(string connectionString, string serviceType, IServiceInfo serviceInfo)
    {
        ConnectionString = connectionString;
        Name = serviceType + serviceInfo?.Id?.Insert(0, "-");
    }

    /// <summary>
    /// Gets or sets the connection string for this connection.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the name of this connection.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets a list of additional properties for this connection.
    /// </summary>
    public Dictionary<string, string> Properties { get; } = new ();
}
