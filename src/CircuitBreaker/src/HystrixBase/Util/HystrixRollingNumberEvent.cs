// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Util;

public enum HystrixRollingNumberEvent
{
    SUCCESS,
    FAILURE,
    TIMEOUT,
    SHORT_CIRCUITED,
    THREAD_POOL_REJECTED,
    SEMAPHORE_REJECTED,
    BAD_REQUEST,
    FALLBACK_SUCCESS,
    FALLBACK_FAILURE,
    FALLBACK_REJECTION,
    FALLBACK_MISSING,
    EXCEPTION_THROWN,
    COMMAND_MAX_ACTIVE,
    EMIT,
    FALLBACK_EMIT,
    THREAD_EXECUTION,
    THREAD_MAX_ACTIVE,
    COLLAPSED,
    RESPONSE_FROM_CACHE,
    COLLAPSER_REQUEST_BATCHED,
    COLLAPSER_BATCH
}