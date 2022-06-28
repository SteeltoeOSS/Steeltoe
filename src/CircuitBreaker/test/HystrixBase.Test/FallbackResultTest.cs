// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public enum FallbackResultTest
{
    UNIMPLEMENTED,
    SUCCESS,
    FAILURE,
    ASYNC_FAILURE,
    MULTIPLE_EMITS_THEN_SUCCESS,
    MULTIPLE_EMITS_THEN_FAILURE,
    NO_EMITS_THEN_SUCCESS
}
