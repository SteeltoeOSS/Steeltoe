// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings;

/// <summary>
/// Provides a method to read the CloudFoundry service bindings in JSON format.
/// </summary>
public interface IServiceBindingsReader
{
    /// <summary>
    /// Returns the CloudFoundry service bindings in JSON format.
    /// </summary>
    /// <returns>
    /// The JSON, or <c>null</c> when unavailable.
    /// </returns>
    string? GetServiceBindingsJson();
}
