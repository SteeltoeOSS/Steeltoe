// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connectors;

public sealed class ConnectorAddOptions
{
    /// <summary>
    /// Gets or sets the callback that creates the driver-specific connection object, which is invoked when
    /// <see cref="Connector{TOptions,TConnection}.GetConnection" /> is called.
    /// </summary>
    public ConnectorCreateConnection CreateConnection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the result of <see cref="CreateConnection" /> is cached and returned on subsequent calls to
    /// <see cref="Connector{TOptions,TConnection}.GetConnection" />. The default value varies per driver, based on documented best practices.
    /// </summary>
    public bool CacheConnection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether contribution to health checks is enabled for this connector. <c>true</c> by default.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Gets or sets the callback that creates an <see cref="IHealthContributor" /> for this connector, in case <see cref="EnableHealthChecks" /> is
    /// <c>true</c>.
    /// </summary>
    public ConnectorCreateHealthContributor CreateHealthContributor { get; set; }

    internal ConnectorAddOptions(ConnectorCreateConnection createConnection, ConnectorCreateHealthContributor createHealthContributor)
    {
        ArgumentGuard.NotNull(createConnection);
        ArgumentGuard.NotNull(createHealthContributor);

        CreateConnection = createConnection;
        CreateHealthContributor = createHealthContributor;
    }
}
