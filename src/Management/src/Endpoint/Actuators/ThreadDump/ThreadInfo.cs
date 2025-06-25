// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common.CasingConventions;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

public sealed class ThreadInfo
{
    // Not available in .NET
    [JsonPropertyName("blockedCount")]
    public long? BlockedCount { get; set; }

    // Not available in .NET
    [JsonPropertyName("blockedTime")]
    public long? BlockedTime { get; set; }

    // Not available in .NET
    [JsonPropertyName("lockedMonitors")]
    public IList<object>? LockedMonitors { get; }

    // Not available in .NET
    [JsonPropertyName("lockedSynchronizers")]
    public IList<object>? LockedSynchronizers { get; }

    // Not available in .NET
    [JsonPropertyName("lockInfo")]
    public object? LockInfo { get; set; }

    // Not available in .NET
    [JsonPropertyName("lockName")]
    public string? LockName { get; set; }

    // Not available in .NET
    [JsonPropertyName("lockOwnerId")]
    public long? LockOwnerId { get; set; }

    // Not available in .NET
    [JsonPropertyName("lockOwnerName")]
    public string? LockOwnerName { get; set; }

    [JsonPropertyName("stackTrace")]
    public IList<StackTraceElement> StackTrace { get; } = new List<StackTraceElement>();

    [JsonPropertyName("threadId")]
    public long ThreadId { get; set; }

    // Not available in .NET, but Apps Manager crashes without it.
    [JsonPropertyName("threadName")]
    public string? ThreadName { get; set; }

    [JsonPropertyName("threadState")]
    [JsonConverter(typeof(SnakeCaseAllCapsEnumMemberJsonConverter))]
    public State ThreadState { get; set; }

    // Not available in .NET
    [JsonPropertyName("waitedCount")]
    public long? WaitedCount { get; set; }

    // Not available in .NET
    [JsonPropertyName("waitedTime")]
    public long? WaitedTime { get; set; }

    [JsonPropertyName("inNative")]
    public bool IsInNative { get; set; }

    // Not available in .NET
    [JsonPropertyName("suspended")]
    public bool? IsSuspended { get; set; }

    public override string ToString()
    {
        string source = IsInNative ? "native" : "managed";
        return $"{ThreadId:D5}, {ThreadState}, {source}, {StackTrace.Count} frames";
    }
}
