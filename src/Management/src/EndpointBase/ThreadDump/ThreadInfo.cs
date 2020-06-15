// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class ThreadInfo
    {
        [JsonProperty("blockedCount")]
        public long BlockedCount { get; set; } = 0; // Not available

        [JsonProperty("blockedTime")]
        public long BlockedTime { get; set; } = -1;  // Not available

        [JsonProperty("lockedMonitors")]
        public List<MonitorInfo> LockedMonitors { get; set; }

        [JsonProperty("lockedSynchronizers")]
        public List<LockInfo> LockedSynchronizers { get; set; }

        [JsonProperty("lockInfo")]
        public LockInfo LockInfo { get; set; }

        [JsonProperty("lockName")]
        public string LockName { get; set; }

        [JsonProperty("lockOwnerId")]
        public long LockOwnerId { get; set; } = -1;

        [JsonProperty("lockOwnerName")]
        public string LockOwnerName { get; set; }

        [JsonProperty("stackTrace")]
        public List<StackTraceElement> StackTrace { get; set; }

        [JsonProperty("threadId")]
        public long ThreadId { get; set; }

        [JsonProperty("threadName")]
        public string ThreadName { get; set; }

        [JsonProperty("threadState")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TState ThreadState { get; set; }

        [JsonProperty("waitedCount")]
        public long WaitedCount { get; set; } = 0; // Not available

        [JsonProperty("waitedTime")]
        public long WaitedTime { get; set; } = -1; // Not available

        [JsonProperty("inNative")]
        public bool IsInNative { get; set; }

        [JsonProperty("suspended")]
        public bool IsSuspended { get; set; }
    }
}
