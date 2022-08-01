// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Steeltoe.Management.Endpoint.Internal;

internal sealed class EndpointMappingEntry
{
    public Action<IEndpointRouteBuilder, Action<IEndpointConventionBuilder>> SetupConvention { get; set; }

    public Action<IEndpointRouteBuilder, EndpointCollectionConventionBuilder> Setup { get; set; }
}
