// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using RichardSzalay.MockHttp;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Implements <see cref="HttpClientHandler" />, so all of its properties are accessible for assertions, but delegates sending to
/// <see cref="MockHttpMessageHandler" />, which makes it easy to set expectations.
/// </summary>
public sealed class DelegateToMockHttpClientHandler : HttpClientHandler
{
    // We want to forward to another HttpClient, but that fails because the outer client has already marked the message as sent.
    // To avoid the error "The request message was already sent...", we need to revert that mark.
    private static readonly FieldInfo SendStatusField = typeof(HttpRequestMessage).GetField("_sendStatus", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public MockHttpMessageHandler Mock { get; } = new();

    public DelegateToMockHttpClientHandler Setup(Action<MockHttpMessageHandler> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        action(Mock);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        MarkMessageAsNotYetSent(request);

        using var httpClient = Mock.ToHttpClient();
        return await httpClient.SendAsync(request, cancellationToken);
    }

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        MarkMessageAsNotYetSent(request);

        using var httpClient = Mock.ToHttpClient();
        return httpClient.Send(request, cancellationToken);
    }

    private static void MarkMessageAsNotYetSent(HttpRequestMessage request)
    {
        SendStatusField.SetValue(request, 0);
    }
}
