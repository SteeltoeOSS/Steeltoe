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

using Steeltoe.Discovery.Eureka.AppInfo;
using System;

namespace Steeltoe.Discovery.Eureka
{
    public class StatusChangedArgs : EventArgs
    {
        public InstanceStatus Previous { get; private set; }

        public InstanceStatus Current { get; private set; }

        public string InstanceId { get; private set; }

        public StatusChangedArgs(InstanceStatus prev, InstanceStatus current, string instanceId)
        {
            Previous = prev;
            Current = current;
            InstanceId = instanceId;
        }
    }
}
