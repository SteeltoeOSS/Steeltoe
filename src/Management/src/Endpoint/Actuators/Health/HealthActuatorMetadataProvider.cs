// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.Health;

internal sealed class HealthActuatorMetadataProvider(string defaultContentType)
    : ActuatorMetadataProvider(defaultContentType)
{
    public override EndpointMetadataCollection GetMetadata(string httpMethod)
    {
        EndpointMetadataCollection baseMetadata = base.GetMetadata(httpMethod);

        var parameterDescription = new ApiParameterDescription
        {
            Name = ManagementOptions.UseStatusCodeFromResponseHeaderName,
            Source = BindingSource.Header,
            IsRequired = false
        };

        List<object> healthMetadata =
        [
            ..baseMetadata,
            parameterDescription
        ];

        return new EndpointMetadataCollection(healthMetadata);
    }
}
