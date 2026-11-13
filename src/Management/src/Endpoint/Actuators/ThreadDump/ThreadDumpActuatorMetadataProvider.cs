// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

internal sealed class ThreadDumpActuatorMetadataProvider(string defaultContentType, string plainTextContentType)
    : ActuatorMetadataProvider(defaultContentType)
{
    public override EndpointMetadataCollection GetMetadata(string httpMethod)
    {
        EndpointMetadataCollection baseMetadata = base.GetMetadata(httpMethod);

        List<object> metadata = baseMetadata.Select(item => item is ProducesAttribute ? new ProducesAttribute(DefaultContentType, plainTextContentType) : item)
            .ToList();

        return new EndpointMetadataCollection(metadata);
    }
}
