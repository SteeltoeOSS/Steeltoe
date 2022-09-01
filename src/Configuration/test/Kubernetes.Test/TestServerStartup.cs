// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test;

public sealed class TestServerStartup
{
    public void Configure(IApplicationBuilder app)
    {
        app.Run(context =>
        {
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        });
    }
}
