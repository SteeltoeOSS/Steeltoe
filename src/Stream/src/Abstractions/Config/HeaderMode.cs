// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Stream.Config;

/// <summary>
/// Specifies how headers are handled during message sending
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public enum HeaderMode
{
    /// <summary>
    /// No headers
    /// </summary>
    None,

    /// <summary>
    /// Native headers
    /// </summary>
    Headers,

    /// <summary>
    /// Headers embedded in payload e.g. kafka less than 0.11
    /// </summary>
    EmbeddedHeaders
}