// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.ServiceRegistry;

public sealed class ServiceRegistryResponse
{
    /// <summary>
    /// Gets a response indicating that this app is not registered with a discovery server.
    /// </summary>
    public static ServiceRegistryResponse NotRegistered { get; } = new(false, null, null);

    /// <summary>
    /// Gets a value indicating whether this app is registered with a discovery server.
    /// </summary>
    public bool IsRegistered { get; }

    /// <summary>
    /// Gets the status of this app in the discovery server, or <c>null</c> after the status has been changed.
    /// </summary>
    public object? Status { get; }

    /// <summary>
    /// Gets the reason why the request could not be handled, or <c>null</c> when successful.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistryResponse" /> class for a successful request.
    /// </summary>
    /// <param name="status">
    /// The status of this app in the discovery server, or <c>null</c> after the status has been changed.
    /// </param>
    public ServiceRegistryResponse(object? status)
        : this(true, status, null)
    {
    }

    private ServiceRegistryResponse(bool isRegistered, object? status, string? error)
    {
        IsRegistered = isRegistered;
        Status = status;
        Error = error;
    }

    /// <summary>
    /// Creates a response for a request that could not be handled, because it is invalid.
    /// </summary>
    /// <param name="error">
    /// The reason why the request could not be handled.
    /// </param>
    public static ServiceRegistryResponse FromError(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        return new ServiceRegistryResponse(true, null, error);
    }
}
