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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.HeapDump
{
    public class HeapDumpEndpointMiddleware : EndpointMiddleware<string>
    {
        private RequestDelegate _next;

        public HeapDumpEndpointMiddleware(RequestDelegate next, HeapDumpEndpoint endpoint, ILogger<HeapDumpEndpointMiddleware> logger = null)
            : base(endpoint, logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsHeapDumpRequest(context))
            {
                await HandleHeapDumpRequestAsync(context);
            }
            else
            {
                await _next(context);
            }
        }

        protected internal async Task HandleHeapDumpRequestAsync(HttpContext context)
        {
            var filename = endpoint.Invoke();
            logger?.LogDebug("Returning: {0}", filename);
            context.Response.Headers.Add("Content-Type", "application/octet-stream");

            if (!File.Exists(filename))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            string gzFilename = filename + ".gz";
            var result = await CompresssAsync(filename, gzFilename);

            if (result != null)
            {
                using (result)
                {
                    context.Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + Path.GetFileName(gzFilename) + "\"");
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    context.Response.ContentLength = result.Length;
                    await result.CopyToAsync(context.Response.Body);
                }

                File.Delete(gzFilename);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        }

        protected internal async Task<Stream> CompresssAsync(string filename, string gzFilename)
        {
            try
            {
                using (var input = new FileStream(filename, FileMode.Open))
                {
                    using (var output = new FileStream(gzFilename, FileMode.CreateNew))
                    {
                        using (var gzipStream = new GZipStream(output, CompressionLevel.Fastest))
                        {
                            await input.CopyToAsync(gzipStream);
                        }
                    }
                }

                return new FileStream(gzFilename, FileMode.Open);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Unable to compress dump");
            }
            finally
            {
                File.Delete(filename);
            }

            return null;
        }

        protected internal bool IsHeapDumpRequest(HttpContext context)
        {
            if (!context.Request.Method.Equals("GET"))
            {
                return false;
            }

            PathString path = new PathString(endpoint.Path);
            return context.Request.Path.Equals(path);
        }
    }
}
