// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient.Test;

public class MyMiddleware : IMiddleware
{
    private static readonly ISet<string> KeyNames = new[]
    {
        "name",
        "healthUrl",
        "managementUrl",
        "serviceUrl",
        "metadata"
    }.ToHashSet();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.Value.EndsWith("instances"))
        {
            var dictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(context.Request.Body);
            context.Response.Headers.Add("Content-Type", "application/json");

            bool isValid = dictionary != null && KeyNames.All(dictionary.ContainsKey);

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
