// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.EndpointBase;
using System;
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

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public HeapDumpEndpointMiddleware(RequestDelegate next, HeapDumpEndpoint endpoint, ILogger<HeapDumpEndpointMiddleware> logger = null)
            : base(endpoint, logger: logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await HandleHeapDumpRequestAsync(context).ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
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

            string gzFilename = filename + ".gz";
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
