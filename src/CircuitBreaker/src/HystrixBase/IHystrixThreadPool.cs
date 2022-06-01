// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix;

public interface IHystrixThreadPool : IDisposable
{
    IHystrixTaskScheduler GetScheduler();

    TaskScheduler GetTaskScheduler();

    void MarkThreadExecution();

    void MarkThreadCompletion();

    void MarkThreadRejection();

    bool IsQueueSpaceAvailable { get; }

    bool IsShutdown { get; }
}
