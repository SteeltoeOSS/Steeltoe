// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.DbMigrations;

internal sealed class DbMigrationsEndpointMiddleware : EndpointMiddleware<object, Dictionary<string, DbMigrationsDescriptor>>
{
    public DbMigrationsEndpointMiddleware(IDbMigrationsEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger<DbMigrationsEndpointMiddleware> logger)
        : base(endpointHandler, managementOptions, logger)
    {
    }

    protected override async Task<Dictionary<string, DbMigrationsDescriptor>> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        return await EndpointHandler.InvokeAsync(null, cancellationToken);
    }
}
