// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class AtomicBoolean
    {
        private volatile int _value;

        public AtomicBoolean()
            : this(false)
        {
        }

        public AtomicBoolean(bool value)
        {
            Value = value;
        }

        public bool Value
        {
            get
            {
                return _value != 0;
            }

            set
            {
                this._value = value ? 1 : 0;
            }
        }

        public bool CompareAndSet(bool expected, bool update)
        {
            int expectedInt = expected ? 1 : 0;
            int updateInt = update ? 1 : 0;
            return Interlocked.CompareExchange(ref this._value, updateInt, expectedInt) == expectedInt;
        }
    }
}
