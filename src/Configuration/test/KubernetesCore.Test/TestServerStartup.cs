// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Extensions.Configuration.Kubernetes.Test
{
    public class TestServerStartup
    {
        public TestServerStartup()
        {
            LastRequest = null;
        }

        public static string Response { get; set; }

        public static int ReturnStatus { get; set; } = 200;

        public static HttpRequest LastRequest { get; set; }

        public static int RequestCount { get; set; } = 0;

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                LastRequest = context.Request;
                RequestCount++;
                context.Response.StatusCode = ReturnStatus;
                await context.Response.WriteAsync(Response);
            });
        }
    }
}
