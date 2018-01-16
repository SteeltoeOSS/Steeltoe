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

using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class ThreadInfo
    {
        public long BlockedCount { get; set; } = 0; // Not available

        public long BlockedTime { get; set; } = -1;  // Not available

        public List<MonitorInfo> LockedMonitors { get; set; }

        public List<LockInfo> LockedSynchronizers { get; set; }

        public LockInfo LockInfo { get; set; }

        public string LockName { get; set; }

        public long LockOwnerId { get; set; }

        public string LockOwnerName { get; set; }

        public List<StackTraceElement> StackTrace { get; set; }

        public long ThreadId { get; set; }

        public string ThreadName { get; set; }

        public TState ThreadState { get; set; }

        public long WaitedCount { get; set; } = 0; // Not available

        public long WaitedTime { get; set; } = -1; // Not available

        public bool IsInNative { get; set; }

        public bool IsSuspended { get; set; }
    }
}
