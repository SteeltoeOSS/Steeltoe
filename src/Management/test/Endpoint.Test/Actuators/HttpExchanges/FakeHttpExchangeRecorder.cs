// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

internal sealed class FakeHttpExchangeRecorder : IHttpExchangeRecorder
{
    private readonly IList<HttpExchange> _httpExchanges;

    public FakeHttpExchangeRecorder(IList<HttpExchange> httpExchanges)
    {
        ArgumentNullException.ThrowIfNull(httpExchanges);

        _httpExchanges = httpExchanges;
    }

    public void HandleRecording(Action<HttpExchange> handler)
    {
        foreach (HttpExchange httpExchange in _httpExchanges)
        {
            handler(httpExchange);
        }
    }
}
