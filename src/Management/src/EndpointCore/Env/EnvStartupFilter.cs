// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Steeltoe.Management.Endpoint.Env
{
    public class EnvStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseEndpoints(endpoints =>
                {
                    endpoints.Map<EnvEndpoint>();
                });
                next(app);
            };
        }
    }
}
