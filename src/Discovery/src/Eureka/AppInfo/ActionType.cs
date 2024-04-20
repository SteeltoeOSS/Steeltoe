// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

/// <summary>
/// Lists the types of change used when fetching the delta of Eureka applications.
/// </summary>
[JsonConverter(typeof(SnakeCaseAllCapsEnumMemberJsonConverter))]
public enum ActionType
{
    /// <summary>
    /// Added to Eureka server, since the last fetch.
    /// </summary>
    Added,

    /// <summary>
    /// Changed in Eureka server, since the last fetch.
    /// </summary>
    Modified,

    /// <summary>
    /// Deleted from Eureka server, since the last fetch.
    /// </summary>
    Deleted
}
