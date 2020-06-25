﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class ThreadDumpStartupFilter : IStartupFilter
    {
        private MediaTypeVersion MediaTypeVersion { get; set; }

        public ThreadDumpStartupFilter(MediaTypeVersion mediaTypeVersion = MediaTypeVersion.V2)
        {
            MediaTypeVersion = mediaTypeVersion;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
                app.UseEndpoints(endpoints =>
                {
                    switch (MediaTypeVersion)
                    {
                        case MediaTypeVersion.V1: endpoints.Map<ThreadDumpEndpoint>(); break;
                        case MediaTypeVersion.V2: endpoints.Map<ThreadDumpEndpoint_v2>(); break;
                    }
                });
            };
        }
    }
}
