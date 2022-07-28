// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

/// <summary>
/// Defines styles for text conversion to snake-case naming convention.
/// </summary>
public enum SnakeCaseStyle
{
    /// <summary>
    /// Indicates all characters uppercase, for example: OUT_OF_SERVICE.
    /// </summary>
    AllCaps,

    /// <summary>
    /// Indicates all characters lowercase, for example: out_of_service.
    /// </summary>
    NoCaps
}
