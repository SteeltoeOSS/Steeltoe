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

namespace Steeltoe.CircuitBreaker.Hystrix.Exceptions
{
    public class HystrixRuntimeException : Exception
    {
        private readonly Type commandClass;
        private readonly Exception fallbackException;
        private readonly FailureType failureCause;

        public HystrixRuntimeException(FailureType failureCause, Type commandClass, string message)
            : base(message)
        {
            this.failureCause = failureCause;
            this.commandClass = commandClass;
            this.fallbackException = null;
        }

        public HystrixRuntimeException(FailureType failureCause, Type commandClass, string message, Exception cause, Exception fallbackException)
            : base(message, cause)
        {
            this.failureCause = failureCause;
            this.commandClass = commandClass;
            this.fallbackException = fallbackException;
        }

        public FailureType FailureType
        {
            get { return failureCause; }
        }

        public Exception FallbackException
        {
            get { return fallbackException;  }
        }

        public Type ImplementingClass
        {
            get { return commandClass;  }
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
    }
}
