// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix;

public enum HystrixEventType
{
    EMIT,
    SUCCESS,
    FAILURE,
    TIMEOUT,
    BAD_REQUEST,
    SHORT_CIRCUITED,
    THREAD_POOL_REJECTED,
    SEMAPHORE_REJECTED,
    FALLBACK_EMIT,
    FALLBACK_SUCCESS,
    FALLBACK_FAILURE,
    FALLBACK_REJECTION,
    FALLBACK_MISSING,
    EXCEPTION_THROWN,
    RESPONSE_FROM_CACHE,
    CANCELLED,
    COLLAPSED
}