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
using System.IO;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class TestConfigServerStartup
    {
        public static string Response { get; set; }

        public static int ReturnStatus { get; set; } = 200;

        public static HttpRequest LastRequest { get; set; }

        public static Stream RequestBody { get; set; }

        public TestConfigServerStartup()
        {
            LastRequest = null;
            RequestBody = new MemoryStream();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                LastRequest = context.Request;
                LastRequest.Body.CopyTo(RequestBody);
                RequestBody.Seek(0, SeekOrigin.Begin);
                context.Response.StatusCode = ReturnStatus;
                await context.Response.WriteAsync(Response);
            });
        }
    }
}
