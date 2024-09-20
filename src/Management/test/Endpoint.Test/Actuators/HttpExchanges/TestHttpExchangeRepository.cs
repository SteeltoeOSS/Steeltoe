// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

internal sealed class TestHttpExchangeRepository : IHttpExchangesRepository
{
    public bool GetHttpExchangesCalled { get; private set; }

    public HttpExchangesResult GetHttpExchanges()
    {
        GetHttpExchangesCalled = true;
        return new HttpExchangesResult(Array.Empty<HttpExchange>());
    }
}
