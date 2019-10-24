// Copyright 2017 the original author or authors.
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

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class TestConfigServerStartup
    {
        public static void Reset()
        {
            Response = null;
            ReturnStatus = new int[1] { 200 };
            LastRequest = null;
            RequestCount = 0;
            Label = AppName = Env = string.Empty;
        }

        public TestConfigServerStartup()
        {
        }

        public static string Response { get; set; }

        public static int[] ReturnStatus { get; set; } = new int[1] { 200 };

        public static HttpRequestInfo LastRequest { get; set; }

        public static int RequestCount { get; set; } = 0;

        public static string Label { get; set; } = string.Empty;

        public static string AppName { get; set; } = string.Empty;

        public static string Env { get; set; } = string.Empty;

        public void Configure(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                LastRequest = new HttpRequestInfo(context.Request);
                context.Response.StatusCode = GetStatusCode(context.Request.Path);
                RequestCount++;
                if (context.Response.StatusCode == 200)
                {
                    await context.Response.WriteAsync(Response);
                }
            });
        }

        public int GetStatusCode(string path)
        {
            if (!string.IsNullOrEmpty(Label))
            {
                if (!path.Contains(Label))
                {
                    return 404;
                }
            }

            if (!string.IsNullOrEmpty(Env))
            {
                if (!path.Contains(Env))
                {
                    return 404;
                }
            }

            if (!string.IsNullOrEmpty(AppName))
            {
                if (!path.Contains(AppName))
                {
                    return 404;
                }
            }

            return ReturnStatus[RequestCount];
        }
    }
}
