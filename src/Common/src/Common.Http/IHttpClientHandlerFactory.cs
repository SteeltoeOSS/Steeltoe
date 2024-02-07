// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.Http;

/// <summary>
/// Provides a method to create a new <see cref="HttpClientHandler" /> instance.
/// </summary>
public interface IHttpClientHandlerFactory
{
    HttpClientHandler Create();
}
