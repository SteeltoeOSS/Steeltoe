//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public class AtomicReferenceArray<T>
    {
        private readonly T[] _array;

        public AtomicReferenceArray(int length)
        {
            this._array = new T[length];
        }
        public T this[int index]
        {
            get
            {
                lock (this._array)
                {
                    return this._array[index];
                }
            }
            set
            {
                lock (this._array)
                {
                    this._array[index] = value;
                }
            }
        }
        public T[] ToArray()
        {
            lock (this._array)
            {
                return (T[])this._array.Clone();
            }
        
        }

        public int Length
        {
            get
            {
                return _array.Length;
            }
        }
    }
}
