// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Steeltoe.Management.Endpoint;

public partial class ActuatorRouteBuilderExtensionsTest
{
    private static void MapEndpoints(Type type, IEndpointRouteBuilder endpoints)
    {
        endpoints.MapBlazorHub(); // https://github.com/SteeltoeOSS/Steeltoe/issues/729
        endpoints.MapActuatorEndpoint(type, convention => convention.RequireAuthorization("TestAuth"));
    }
}
#endif
