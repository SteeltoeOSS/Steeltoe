// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient.Test;

public class MyMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.Value.EndsWith("instances"))
        {
            var kvp = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(context.Request.Body);
            context.Response.Headers.Add("Content-Type", "application/json");

            var isValid = kvp.ContainsKey("name")
                          && kvp.ContainsKey("healthUrl")
                          && kvp.ContainsKey("managementUrl")
                          && kvp.ContainsKey("serviceUrl")
                          && kvp.ContainsKey("metadata");

            // Registration response
            await context.Response.WriteAsync(isValid ? "{\"Id\":\"1234567\"}" : "{\"SerializationError: invalid keys in Application object.\"}");
        }
        else
        {
            // Unregister response
            await context.Response.WriteAsync("Ok!");
        }
    }
}
