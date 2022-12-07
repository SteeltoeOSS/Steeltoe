// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Forwards the request to another <see cref="HttpClient" /> instance.
/// </summary>
public sealed class HttpClientDelegatingHandler : DelegatingHandler
{
    // HttpClient delegates to a HttpMessageHandler, which is why we end up here. Now we want to forward to another HttpClient, which fails because the
    // outer client has already marked the message as sent. To avoid the error "The request message was already sent...", we need to revert that mark.
    private static readonly FieldInfo SendStatusField = typeof(HttpRequestMessage).GetField("_sendStatus", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly HttpClient _httpClient;

    public HttpClientDelegatingHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        MarkMessageAsNotYetSent(request);

        return _httpClient.Send(request, cancellationToken);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        MarkMessageAsNotYetSent(request);

        return _httpClient.SendAsync(request, cancellationToken);
    }

    private static void MarkMessageAsNotYetSent(HttpRequestMessage request)
    {
        SendStatusField.SetValue(request, 0);
    }
}
