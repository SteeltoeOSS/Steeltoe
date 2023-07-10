// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Steeltoe.Connectors;

/// <summary>
/// Represents the method that creates a driver-specific connection object.
/// </summary>
/// <param name="serviceProvider">
/// The application's configured services. Inject <see cref="IOptionsMonitor{ConnectionStringOptions}" /> to obtain the connection string.
/// </param>
/// <param name="serviceBindingName">
/// The service binding name.
/// </param>
/// <returns>
/// The driver-specific connection object.
/// </returns>
public delegate object ConnectorCreateConnection(IServiceProvider serviceProvider, string serviceBindingName);
