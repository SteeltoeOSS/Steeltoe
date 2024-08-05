// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.Http.HttpClientPooling;

/// <summary>
/// Provides a method to create the primary <see cref="HttpClientHandler" /> for a named <see cref="HttpClient" />.
/// </summary>
public sealed class HttpClientHandlerFactory
{
    private HttpClientHandler? _handler;

    /// <summary>
    /// Uses an existing <see cref="HttpClientHandler" /> instance. Typically used to pass a mock from tests.
    /// </summary>
    /// <param name="handler">
    /// The handler to return from <see cref="Create" />.
    /// </param>
    public HttpClientHandlerFactory Using(HttpClientHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
        return this;
    }

    /// <summary>
    /// Creates a new <see cref="HttpClientHandler" /> instance.
    /// </summary>
    public HttpClientHandler Create()
    {
        return _handler ?? new HttpClientHandler();
    }
}
