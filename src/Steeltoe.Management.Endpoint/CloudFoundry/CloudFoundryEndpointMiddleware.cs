//// Copyright 2017 the original author or authors.
////
//// Licensed under the Apache License, Version 2.0 (the "License");
//// you may not use this file except in compliance with the License.
//// You may obtain a copy of the License at
////
//// http://www.apache.org/licenses/LICENSE-2.0
////
//// Unless required by applicable law or agreed to in writing, software
//// distributed under the License is distributed on an "AS IS" BASIS,
//// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//// See the License for the specific language governing permissions and
//// limitations under the License.

//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Primitives;
//using Steeltoe.Management.Endpoint.Middleware;
//using System.Threading.Tasks;

//namespace Steeltoe.Management.Endpoint.CloudFoundry
//{
//    public class CloudFoundryEndpointMiddleware : EndpointMiddleware<Links, string>
//    {
//        private ICloudFoundryOptions _options;
//        private RequestDelegate _next;

//        public CloudFoundryEndpointMiddleware(RequestDelegate next, CloudFoundryEndpoint endpoint, ILogger<CloudFoundryEndpointMiddleware> logger = null)
//            : base(endpoint, logger)
//        {
//            _next = next;
//            _options = endpoint.Options as ICloudFoundryOptions;
//        }

//        public async Task Invoke(HttpContext context)
//        {
//            if (IsCloudFoundryRequest(context))
//            {
//                await HandleCloudFoundryRequestAsync(context);
//            }
//            else
//            {
//                await _next(context);
//            }
//        }

//        protected internal async Task HandleCloudFoundryRequestAsync(HttpContext context)
//        {
//            var serialInfo = base.HandleRequest(GetRequestUri(context.Request));
//            logger?.LogDebug("Returning: {0}", serialInfo);
//            context.Response.Headers.Add("Content-Type", "application/json;charset=UTF-8");
//            await context.Response.WriteAsync(serialInfo);
//        }

//        protected internal string GetRequestUri(HttpRequest request)
//        {
//            string scheme = request.Scheme;

//            if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
//            {
//                scheme = headerScheme.ToString();
//            }

//            return scheme + "://" + request.Host.ToString() + request.Path.ToString();
//        }

//        protected internal bool IsCloudFoundryRequest(HttpContext context)
//        {
//            if (!context.Request.Method.Equals("GET"))
//            {
//                return false;
//            }

//            PathString path = new PathString(endpoint.Path);
//            return context.Request.Path.Equals(path);
//        }
//    }
//}
