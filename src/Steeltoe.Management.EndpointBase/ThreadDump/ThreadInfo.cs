// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
