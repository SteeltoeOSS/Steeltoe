// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;

namespace Steeltoe.Connector.Services;

public interface IServiceInfoFactory
{
    /// <summary>
    /// Check if this factory can create <see cref="IServiceInfo"/> from the given binding
    /// </summary>
    /// <param name="binding">A service binding to evaluate</param>
    /// <returns>Gets a value indicating whether or not the binding is compatible with this factory</returns>
    bool Accepts(Service binding);

    /// <summary>
    /// Return service information from a service binding
    /// </summary>
    /// <param name="binding">A service binding</param>
    /// <returns>Relevant <see cref="IServiceInfo"/></returns>
    IServiceInfo Create(Service binding);
}
