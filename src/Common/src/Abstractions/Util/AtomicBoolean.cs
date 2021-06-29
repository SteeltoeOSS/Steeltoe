﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Steeltoe.Common.Util
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
            get => _value != 0;

            set => _value = value ? 1 : 0;
        }

        public bool CompareAndSet(bool expected, bool update)
        {
            var expectedInt = expected ? 1 : 0;
            var updateInt = update ? 1 : 0;
            return Interlocked.CompareExchange(ref _value, updateInt, expectedInt) == expectedInt;
        }

        public bool GetAndSet(bool newValue)
        {
            var newValueInt = newValue ? 1 : 0;
            var previous = Interlocked.Exchange(ref _value, newValueInt);
            return previous == 1;
        }
    }
}
