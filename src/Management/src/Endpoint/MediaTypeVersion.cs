// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint;

/// <summary>
/// Represents the media type version, used in content negotiation.
/// </summary>
public enum MediaTypeVersion
{
    /// <summary>
    /// Indicates version 1 of the media type.
    /// </summary>
    V1,

    /// <summary>
    /// Indicates version 2 of the media type.
    /// </summary>
    V2,

    /// <summary>
    /// Indicates version 3 of the media type.
    /// </summary>
    V3
}
