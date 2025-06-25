// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Steeltoe.Management.Endpoint.Middleware;

/// <summary>
/// Provides metadata for an actuator endpoint, which is used by the route mappings actuator.
/// </summary>
public class ActuatorMetadataProvider
{
    protected string DefaultContentType { get; }

    public ActuatorMetadataProvider(string defaultContentType)
    {
        ArgumentNullException.ThrowIfNull(defaultContentType);

        DefaultContentType = defaultContentType;
    }

    /// <summary>
    /// Gets metadata for the actuator endpoint, based on the HTTP method. Override to supply actuator-specific metadata.
    /// </summary>
    /// <param name="httpMethod">
    /// The HTTP request method.
    /// </param>
    public virtual EndpointMetadataCollection GetMetadata(string httpMethod)
    {
        ArgumentException.ThrowIfNullOrEmpty(httpMethod);

        List<object> metadata = [];

        if (httpMethod is "DELETE" or "POST" or "PUT" or "PATCH")
        {
            metadata.Add(new ConsumesAttribute(DefaultContentType));
        }

        if (httpMethod != "OPTIONS")
        {
            metadata.Add(new ProducesAttribute(DefaultContentType));
        }

        return new EndpointMetadataCollection(metadata);
    }
}
