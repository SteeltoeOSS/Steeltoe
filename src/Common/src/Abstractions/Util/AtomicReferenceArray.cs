// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public class AtomicReferenceArray<T>
{
    private readonly T[] _array;

    public AtomicReferenceArray(int length)
    {
        _array = new T[length];
    }

    public T this[int index]
    {
        get
        {
            lock (_array)
            {
                return _array[index];
            }
        }

        set
        {
            lock (_array)
            {
                _array[index] = value;
            }
        }
    }

    public T[] ToArray()
    {
        lock (_array)
        {
            return (T[])_array.Clone();
        }
    }

    public int Length
    {
        get
        {
            lock (_array)
            {
                return _array.Length;
            }
        }
    }
}
