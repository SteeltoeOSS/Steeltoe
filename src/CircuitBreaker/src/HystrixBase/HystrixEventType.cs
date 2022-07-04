// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix;

public enum HystrixEventType
{
    Emit,
    Success,
    Failure,
    Timeout,
    BadRequest,
    ShortCircuited,
    ThreadPoolRejected,
    SemaphoreRejected,
    FallbackEmit,
    FallbackSuccess,
    FallbackFailure,
    FallbackRejection,
    FallbackMissing,
    ExceptionThrown,
    ResponseFromCache,
    Cancelled,
    Collapsed
}