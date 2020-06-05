// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Common.Http.Test
{
    public class TestInnerDelegatingHandlerBrokenServer : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            responseMessage.Headers.Add("requestUri", request.RequestUri.AbsoluteUri);
            return Task.Factory.StartNew(() => responseMessage, cancellationToken);
        }
    }
}
