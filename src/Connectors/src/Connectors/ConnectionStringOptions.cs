// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connectors;

/// <summary>
/// Provides configuration settings to connect to a data source.
/// </summary>
public abstract class ConnectionStringOptions
{
    /// <summary>
    /// Gets or sets the connection string for this data source.
    /// </summary>
    public string? ConnectionString { get; set; }
}
