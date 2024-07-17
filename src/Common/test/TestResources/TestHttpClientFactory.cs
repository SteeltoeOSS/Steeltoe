// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources;

public sealed class TestHttpClientFactory : IHttpClientFactory, IDisposable
{
    private readonly HttpClient _httpClient;

    public TestHttpClientFactory()
        : this(null)
    {
    }

    public TestHttpClientFactory(HttpClient? httpClient)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public HttpClient CreateClient(string name)
    {
        return _httpClient;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
