// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.ThreadDump;

public class ThreadInfo
{
    [JsonPropertyName("blockedCount")]
    public long BlockedCount { get; set; } = 0; // Not available

    [JsonPropertyName("blockedTime")]
    public long BlockedTime { get; set; } = -1;  // Not available

    [JsonPropertyName("lockedMonitors")]
    public List<MonitorInfo> LockedMonitors { get; set; }

    [JsonPropertyName("lockedSynchronizers")]
    public List<LockInfo> LockedSynchronizers { get; set; }

    [JsonPropertyName("lockInfo")]
    public LockInfo LockInfo { get; set; }

    [JsonPropertyName("lockName")]
    public string LockName { get; set; }

    [JsonPropertyName("lockOwnerId")]
    public long LockOwnerId { get; set; } = -1;

    [JsonPropertyName("lockOwnerName")]
    public string LockOwnerName { get; set; }

    [JsonPropertyName("stackTrace")]
    public List<StackTraceElement> StackTrace { get; set; }

    [JsonPropertyName("threadId")]
    public long ThreadId { get; set; }

    [JsonPropertyName("threadName")]
    public string ThreadName { get; set; }

    [JsonPropertyName("threadState")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TState ThreadState { get; set; }

    [JsonPropertyName("waitedCount")]
    public long WaitedCount { get; set; } = 0; // Not available

    [JsonPropertyName("waitedTime")]
    public long WaitedTime { get; set; } = -1; // Not available

    [JsonPropertyName("inNative")]
    public bool IsInNative { get; set; }

    [JsonPropertyName("suspended")]
    public bool IsSuspended { get; set; }
}
