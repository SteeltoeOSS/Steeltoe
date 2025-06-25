// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connectors;

/// <summary>
/// Represents the method that creates a driver-specific health contributor.
/// </summary>
/// <param name="serviceProvider">
/// The application's configured services. Inject <see cref="IOptionsMonitor{ConnectionStringOptions}" /> to obtain the connection string.
/// </param>
/// <param name="serviceBindingName">
/// The service binding name.
/// </param>
/// <returns>
/// The driver-specific health contributor.
/// </returns>
public delegate IHealthContributor ConnectorCreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName);
