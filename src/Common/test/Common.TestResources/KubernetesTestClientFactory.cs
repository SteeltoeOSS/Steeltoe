// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Builds <see cref="Kubernetes"/> instances for tests using the same constructor pipeline as the upstream client
/// (<see href="https://github.com/kubernetes-client/csharp/blob/v19.0.2/src/KubernetesClient/Kubernetes.ConfigInit.cs">Kubernetes.ConfigInit.cs</see>).
/// <see cref="Kubernetes.HttpClient"/> has a <c>protected</c> setter, so tests cannot assign an <see cref="HttpClient"/> directly.
/// </summary>
public static class KubernetesTestClientFactory
{
    /// <summary>
    /// Creates a client that routes requests through <paramref name="innerHandler"/> (for example, <c>MockHttpMessageHandler</c>).
    /// </summary>
    /// <param name="configuration">Client configuration (must include a valid <see cref="KubernetesClientConfiguration.Host"/>).</param>
    /// <param name="innerHandler">The terminal handler that produces mock or test responses.</param>
    public static Kubernetes Create(KubernetesClientConfiguration configuration, HttpMessageHandler innerHandler)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (innerHandler is null)
        {
            throw new ArgumentNullException(nameof(innerHandler));
        }

        return new Kubernetes(configuration, new DelegatingHandler[] { new RequestForwardingHandler(innerHandler) });
    }

    private sealed class RequestForwardingHandler : DelegatingHandler
    {
        private readonly HttpMessageInvoker _invoker;

        public RequestForwardingHandler(HttpMessageHandler inner) =>
            _invoker = new HttpMessageInvoker(inner, disposeHandler: false);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _invoker.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _invoker.SendAsync(request, cancellationToken);
    }
}
