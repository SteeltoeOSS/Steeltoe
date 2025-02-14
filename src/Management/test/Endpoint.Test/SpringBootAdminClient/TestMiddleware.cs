// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Management.Endpoint.Test.SpringBootAdminClient;

public sealed class TestMiddleware : IMiddleware
{
    private static readonly HashSet<string> KeyNames =
    [
        "name",
        "healthUrl",
        "managementUrl",
        "serviceUrl",
        "metadata"
    ];

    public async Task InvokeAsync(HttpContext context, RequestDelegate? next)
    {
        if (context.Request.Path.Value?.EndsWith("instances", StringComparison.Ordinal) == true)
        {
            var dictionary = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(context.Request.Body, cancellationToken: context.RequestAborted);
            context.Response.Headers.Append("Content-Type", "application/json");

            bool isValid = dictionary != null && KeyNames.All(dictionary.ContainsKey);

            // Registration response
            await context.Response.WriteAsync(isValid ? """{"Id":"1234567"}""" : """{"SerializationError: invalid keys in Application object."}""");
        }
        else
        {
            // Unregister response
            await context.Response.WriteAsync("Ok!");
        }
    }
}
