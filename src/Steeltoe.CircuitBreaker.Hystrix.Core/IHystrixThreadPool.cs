//
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

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System;
using System.Threading.Tasks;


namespace Steeltoe.CircuitBreaker.Hystrix
{
    public interface IHystrixThreadPool : IDisposable
    {

        IHystrixTaskScheduler GetScheduler();

        TaskScheduler GetTaskScheduler();

        /// <summary>
        /// Mark when a thread begins executing a command.
        /// </summary>
        void MarkThreadExecution();

        /// <summary>
        /// Mark when a thread completes executing a command.
        /// </summary>
        void MarkThreadCompletion();

        /// <summary>
        /// Mark when a command gets rejected from the threadpool
        /// </summary>
        void MarkThreadRejection();

        /// <summary>
        /// Whether the queue will allow adding an item to it.
        /// <para>
        /// This allows dynamic control of the max queueSize versus whatever the actual max queueSize is so that dynamic changes can be done via property changes rather than needing an app
        /// restart to adjust when commands should be rejected from queuing up.
        /// 
        /// </para>
        /// </summary>
        /// <returns> boolean whether there is space on the queue </returns>
        bool IsQueueSpaceAvailable { get; }

        bool IsShutdown { get; }
    }
}