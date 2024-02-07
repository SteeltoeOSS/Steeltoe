// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.Http;

/// <summary>
/// Creates a new <see cref="HttpClientHandler" /> instance.
/// </summary>
public sealed class DefaultHttpClientHandlerFactory : IHttpClientHandlerFactory
{
    private static readonly Func<HttpClientHandler> DefaultCreateAction = () => new HttpClientHandler();
    private Func<HttpClientHandler>? _createAction;

    internal void Using(Func<HttpClientHandler> createAction)
    {
        ArgumentGuard.NotNull(createAction);

        _createAction = createAction;
    }

    public HttpClientHandler Create()
    {
        return _createAction != null ? _createAction() : DefaultCreateAction();
    }
}
