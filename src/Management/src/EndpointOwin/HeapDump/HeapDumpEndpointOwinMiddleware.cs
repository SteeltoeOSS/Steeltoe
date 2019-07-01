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

using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.EndpointBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointOwin.HeapDump
{
    public class HeapDumpEndpointOwinMiddleware : EndpointOwinMiddleware<string>
    {
        protected new HeapDumpEndpoint _endpoint;

        public HeapDumpEndpointOwinMiddleware(OwinMiddleware next, HeapDumpEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<HeapDumpEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, mgmtOptions, logger: logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public HeapDumpEndpointOwinMiddleware(OwinMiddleware next, HeapDumpEndpoint endpoint, ILogger<HeapDumpEndpointOwinMiddleware> logger = null)
            : base(next, endpoint, logger: logger)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (!RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await Next.Invoke(context).ConfigureAwait(false);
            }
            else
            {
                await HandleHeapDumpRequestAsync(context).ConfigureAwait(false);
            }
        }

        protected internal async Task HandleHeapDumpRequestAsync(IOwinContext context)
        {
            var filename = _endpoint.Invoke();
            _logger?.LogDebug("Returning: {0}", filename);
            context.Response.Headers.SetValues("Content-Type", new string[] { "application/octet-stream" });

            if (!File.Exists(filename))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }

            string gzFilename = filename + ".gz";
            var result = await Utils.CompressFileAsync(filename, gzFilename).ConfigureAwait(false);

            if (result != null)
            {
                using (result)
                {
                    context.Response.Headers.Add("Content-Disposition", new string[] { "attachment; filename=\"" + Path.GetFileName(gzFilename) + "\"" });
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentLength = result.Length;
                    await result.CopyToAsync(context.Response.Body).ConfigureAwait(false);
                }

                File.Delete(gzFilename);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
    }
}
