// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IHystrixTaskScheduler : IDisposable
{
    int CurrentActiveCount { get; }

    int CurrentCompletedTaskCount { get; }

    int CurrentCorePoolSize { get; }

    int CurrentLargestPoolSize { get; }

    int CurrentMaximumPoolSize { get; }

    int CurrentPoolSize { get; }

    int CurrentQueueSize { get; }

    int CurrentTaskCount { get; }

    int CorePoolSize { get; set; }

    int MaximumPoolSize { get; set; }

    TimeSpan KeepAliveTime { get; set; }

    bool IsQueueSpaceAvailable { get; }

    bool IsShutdown { get; }
}