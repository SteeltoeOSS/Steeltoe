// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class AtomicReference<T>
        where T : class
    {
        private volatile T _value;

        public AtomicReference()
            : this(default)
        {
        }

        public AtomicReference(T value)
        {
            _value = value;
        }

        public T Value
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;
            }
        }

        public bool CompareAndSet(T expected, T update)
        {
            return Interlocked.CompareExchange(ref _value, update, expected) == expected;
        }

        public T GetAndSet(T value)
        {
            return Interlocked.Exchange(ref _value, value);
        }
    }
}
