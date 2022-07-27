// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class MonitorInfo : LockInfo
{
    [JsonPropertyName("lockedStackDepth")]
    public int LockedStackDepth { get; set; }

    [JsonPropertyName("lockedStackFrame")]
    public StackTraceElement LockedStackFrame { get; set; }
}