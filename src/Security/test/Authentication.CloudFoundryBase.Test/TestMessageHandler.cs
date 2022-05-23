// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class TestMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage LastRequest { get; set; }

        public HttpResponseMessage Response { get; set; } = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(Response);
        }
    }
}
