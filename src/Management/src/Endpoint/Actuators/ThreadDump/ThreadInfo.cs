// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

public sealed class ThreadInfo
{
    [JsonPropertyName("blockedCount")]
    public long BlockedCount { get; set; } // Not available

    [JsonPropertyName("blockedTime")]
    public long BlockedTime { get; set; } = -1; // Not available

    [JsonPropertyName("lockedMonitors")]
    public IList<object>? LockedMonitors { get; } // Not available

    [JsonPropertyName("lockedSynchronizers")]
    public IList<object>? LockedSynchronizers { get; } // Not available

    [JsonPropertyName("lockInfo")]
    public object? LockInfo { get; set; }

    [JsonPropertyName("lockName")]
    public string? LockName { get; set; }

    [JsonPropertyName("lockOwnerId")]
    public long LockOwnerId { get; set; } = -1;

    [JsonPropertyName("lockOwnerName")]
    public string? LockOwnerName { get; set; }

    [JsonPropertyName("stackTrace")]
    public IList<StackTraceElement> StackTrace { get; } = new List<StackTraceElement>();

    [JsonPropertyName("threadId")]
    public long ThreadId { get; set; }

    [JsonPropertyName("threadName")]
    public string? ThreadName { get; set; }

    [JsonPropertyName("threadState")]
    public State ThreadState { get; set; }

    [JsonPropertyName("waitedCount")]
    public long WaitedCount { get; set; } // Not available

    [JsonPropertyName("waitedTime")]
    public long WaitedTime { get; set; } = -1; // Not available

    [JsonPropertyName("inNative")]
    public bool IsInNative { get; set; }

    [JsonPropertyName("suspended")]
    public bool IsSuspended { get; set; }
}
