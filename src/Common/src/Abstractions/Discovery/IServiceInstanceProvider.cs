// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Common.Discovery;

public interface IServiceInstanceProvider
{
    /// <summary>
    /// Gets a human readable description of the implementation.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets all known service Ids.
    /// </summary>
    IList<string> Services { get; }

    /// <summary>
    /// Get all ServiceInstances associated with a particular serviceId.
    /// </summary>
    /// <param name="serviceId">the serviceId to lookup.</param>
    /// <returns>List of service instances.</returns>
    IList<IServiceInstance> GetInstances(string serviceId);
}
