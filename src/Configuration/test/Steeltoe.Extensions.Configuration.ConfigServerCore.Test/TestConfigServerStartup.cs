// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Extensions.Configuration.ConfigServerCore.Test
{
    public class TestConfigServerStartup
    {
        public TestConfigServerStartup()
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
