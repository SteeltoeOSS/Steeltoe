// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

[JsonConverter(typeof(SnakeCaseAllCapsEnumMemberJsonConverter))]
public enum ActionType
{
    /// <summary>
    /// Added in the discovery server.
    /// </summary>
    Added,

    /// <summary>
    /// Changed in the discovery server.
    /// </summary>
    Modified,

    /// <summary>
    /// Deleted from the discovery server.
    /// </summary>
    Deleted
}
