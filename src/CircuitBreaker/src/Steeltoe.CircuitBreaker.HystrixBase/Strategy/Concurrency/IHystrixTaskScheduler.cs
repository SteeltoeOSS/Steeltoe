// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency
{
    public interface IHystrixTaskScheduler : IDisposable
    {
        int CurrentActiveCount { get;  }

        int CurrentCompletedTaskCount { get; }

        int CurrentCorePoolSize { get; }

        int CurrentLargestPoolSize { get; }

        int CurrentMaximumPoolSize { get;  }

        int CurrentPoolSize { get; }

        int CurrentQueueSize { get; }

        int CurrentTaskCount { get; }

        int CorePoolSize { get; set; }

        int MaximumPoolSize { get; set; }

        TimeSpan KeepAliveTime { get; set; }

        bool IsQueueSpaceAvailable { get; }

        bool IsShutdown { get; }
    }
}
