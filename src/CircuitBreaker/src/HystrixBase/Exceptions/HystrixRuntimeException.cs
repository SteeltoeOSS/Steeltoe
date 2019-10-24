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

using System;
using System.Runtime.Serialization;

namespace Steeltoe.CircuitBreaker.Hystrix.Exceptions
{
    [Serializable]
    public class HystrixRuntimeException : Exception
    {
        public HystrixRuntimeException(FailureType failureCause, Type commandClass, string message)
            : base(message)
        {
            FailureType = failureCause;
            ImplementingClass = commandClass;
            FallbackException = null;
        }

        public HystrixRuntimeException(FailureType failureCause, Type commandClass, string message, Exception cause, Exception fallbackException)
            : base(message, cause)
        {
            FailureType = failureCause;
            ImplementingClass = commandClass;
            FallbackException = fallbackException;
        }

        public HystrixRuntimeException()
        {
        }

        public HystrixRuntimeException(string message)
            : base(message)
        {
        }

        public HystrixRuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected HystrixRuntimeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public FailureType FailureType { get; }

        public Exception FallbackException { get; }

        public Type ImplementingClass { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
