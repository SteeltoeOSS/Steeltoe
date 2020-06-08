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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.HeapDump
{
    public class HeapDumpEndpointMiddleware : EndpointMiddleware<string>
    {
        private readonly RequestDelegate _next;

        public HeapDumpEndpointMiddleware(RequestDelegate next, HeapDumpEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<HeapDumpEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, logger: logger)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                return HandleHeapDumpRequestAsync(context);
            }

            return _next(context);
        }

        protected internal async Task HandleHeapDumpRequestAsync(HttpContext context)
        {
            var filename = _endpoint.Invoke();
            _logger?.LogDebug("Returning: {0}", filename);
            context.Response.Headers.Add("Content-Type", "application/octet-stream");

            if (!File.Exists(filename))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var gzFilename = filename + ".gz";
            var result = await Utils.CompressFileAsync(filename, gzFilename).ConfigureAwait(false);

            if (result != null)
            {
                using (result)
                {
                    context.Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + Path.GetFileName(gzFilename) + "\"");
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentLength = result.Length;
                    await result.CopyToAsync(context.Response.Body).ConfigureAwait(false);
                }

                File.Delete(gzFilename);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        }
    }
}
