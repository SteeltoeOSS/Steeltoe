// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.ServiceRegistry;

public sealed class ServiceRegistryRequest
{
    /// <summary>
    /// Gets the new status to assign, or <c>null</c> to get the current status.
    /// </summary>
    public string? Status { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistryRequest" /> class for getting the current status.
    /// </summary>
    public ServiceRegistryRequest()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistryRequest" /> class for changing the status.
    /// </summary>
    /// <param name="status">
    /// The new status to assign, for example: "UP" or "OUT_OF_SERVICE".
    /// </param>
    public ServiceRegistryRequest(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        Status = status;
    }

    public override string ToString()
    {
        return Status == null ? "Get" : $"Change: Status={Status}";
    }
}
