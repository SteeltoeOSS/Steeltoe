// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class RequestCacheThreadRejectionWithoutFallbackTaskScheduler : HystrixTaskScheduler
{
    public RequestCacheThreadRejectionWithoutFallbackTaskScheduler(HystrixThreadPoolOptions options)
        : base(options)
    {
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return null;
    }

    protected override void QueueTask(Task task)
    {
        throw new RejectedExecutionException("Rejected command because task queue queueSize is at rejection threshold.");
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        return false;
    }
}
