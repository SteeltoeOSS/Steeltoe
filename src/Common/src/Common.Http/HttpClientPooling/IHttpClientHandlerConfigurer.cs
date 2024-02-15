// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.Http.HttpClientPooling;

/// <summary>
/// Provides a method to configure the primary <see cref="HttpClientHandler" /> for a named <see cref="HttpClient" />.
/// </summary>
public interface IHttpClientHandlerConfigurer
{
    void Configure(HttpClientHandler handler);
}
