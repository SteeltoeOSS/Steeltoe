﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class TestConfigServerStartup
    {
        public static string Host { get; set; }

        public static string Response { get; set; }

        public static int ReturnStatus { get; set; } = 200;

        public static HttpRequestInfo LastRequest { get; set; }

        public TestConfigServerStartup()
        {
            LastRequest = null;
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                if (!string.IsNullOrEmpty(Host))
                {
                    if (!Host.Equals(context.Request.Host.Value))
                    {
                        context.Response.StatusCode = 500;
                        return;
                    }
                }

                LastRequest = new HttpRequestInfo(context.Request);
                context.Response.StatusCode = ReturnStatus;
                await context.Response.WriteAsync(Response);
            });
        }
    }
}
