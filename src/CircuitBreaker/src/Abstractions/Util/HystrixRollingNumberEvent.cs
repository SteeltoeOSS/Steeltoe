// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
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
}