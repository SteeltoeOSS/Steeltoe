// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Configuration.ConfigServer.Test;

internal sealed class ForwardingHttpClientHandler : HttpClientHandler
{
    private readonly HttpMessageInvoker _invoker;

    public ForwardingHttpClientHandler(HttpMessageHandler innerHandler)
    {
        ArgumentGuard.NotNull(innerHandler);

        _invoker = new HttpMessageInvoker(innerHandler, false);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _invoker.SendAsync(request, cancellationToken);
    }
}
