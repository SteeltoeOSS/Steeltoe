// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace Steeltoe.Common.Http.Test;

public class TestInnerDelegatingHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        responseMessage.Headers.Add("requestUri", request.RequestUri.AbsoluteUri);
        return Task.Factory.StartNew(() => responseMessage, cancellationToken);
    }
}
