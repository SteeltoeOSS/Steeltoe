// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Exceptions
{
    public enum FailureType
    {
        BAD_REQUEST_EXCEPTION,
        COMMAND_EXCEPTION,
        TIMEOUT,
        SHORTCIRCUIT,
        REJECTED_THREAD_EXECUTION,
        REJECTED_SEMAPHORE_EXECUTION,
        REJECTED_SEMAPHORE_FALLBACK
    }
}
