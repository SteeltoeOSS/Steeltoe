// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Http;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Discovery.Eureka.Test;

internal sealed class TestableHttpClientHandlerFactory : IHttpClientHandlerFactory
{
    private readonly DelegateToMockHttpClientHandler _handler;

    public TestableHttpClientHandlerFactory(DelegateToMockHttpClientHandler handler)
    {
        _handler = handler;
    }

    public HttpClientHandler Create()
    {
        return _handler;
    }
}
