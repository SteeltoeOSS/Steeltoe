// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.Refresh;

internal sealed class RefreshActuatorMetadataProvider(string defaultContentType)
    : ActuatorMetadataProvider(defaultContentType)
{
    public override EndpointMetadataCollection GetMetadata(string httpMethod)
    {
        List<object> metadata = [new ProducesAttribute(DefaultContentType)];
        return new EndpointMetadataCollection(metadata);
    }
}
