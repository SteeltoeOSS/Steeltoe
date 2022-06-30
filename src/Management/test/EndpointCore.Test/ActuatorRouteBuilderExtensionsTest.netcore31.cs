// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP3_1
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Steeltoe.Management.Endpoint;

public partial class ActuatorRouteBuilderExtensionsTest
{
    private static void MapEndpoints(Type type, IEndpointRouteBuilder endpoints)
    {
        endpoints.MapBlazorHub(); // https://github.com/SteeltoeOSS/Steeltoe/issues/729
#pragma warning disable CS0618 // Type or member is obsolete
        endpoints.MapActuatorEndpoint(type).RequireAuthorization("TestAuth");
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
#endif
