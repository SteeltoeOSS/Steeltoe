// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Extensions.Configuration;

/// <summary>
/// Represents a service instance bound to an application
/// </summary>
public class Service : AbstractServiceOptions
{
    /// <summary>
    /// Gets or sets the connection information and credentials for using the service
    /// </summary>
    public Dictionary<string, Credential> Credentials { get; set; } = new (StringComparer.InvariantCultureIgnoreCase);
}
