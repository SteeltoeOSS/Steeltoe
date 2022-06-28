// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public enum ExecutionResultTest
{
    SUCCESS,
    FAILURE,
    ASYNC_FAILURE,
    HYSTRIX_FAILURE,
    ASYNC_HYSTRIX_FAILURE,
    RECOVERABLE_ERROR,
    ASYNC_RECOVERABLE_ERROR,
    UNRECOVERABLE_ERROR,
    ASYNC_UNRECOVERABLE_ERROR,
    BAD_REQUEST,
    ASYNC_BAD_REQUEST,
    MULTIPLE_EMITS_THEN_SUCCESS,
    MULTIPLE_EMITS_THEN_FAILURE,
    NO_EMITS_THEN_SUCCESS
}
