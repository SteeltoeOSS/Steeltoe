// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Middleware;
internal class ActuatorsMiddleware: IMiddleware
{
    private readonly ActuatorRouter _router;

    public ActuatorsMiddleware(ActuatorRouter router, ILogger<ActuatorsMiddleware> logger = null)
    {
        _router = router;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next) => _router.RouteAsync(context, next);    
  
}
