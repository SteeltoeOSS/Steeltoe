// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Util;

public enum HystrixRollingNumberEvent
{
    Success,
    Failure,
    Timeout,
    ShortCircuited,
    ThreadPoolRejected,
    SemaphoreRejected,
    BadRequest,
    FallbackSuccess,
    FallbackFailure,
    FallbackRejection,
    FallbackMissing,
    ExceptionThrown,
    CommandMaxActive,
    Emit,
    FallbackEmit,
    ThreadExecution,
    ThreadMaxActive,
    Collapsed,
    ResponseFromCache,
    CollapserRequestBatched,
    CollapserBatch
}
