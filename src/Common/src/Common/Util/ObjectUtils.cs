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

namespace Steeltoe.Common.Util
{
    public static class ObjectUtils
    {
        private const int INITIAL_HASH = 7;
        private const int MULTIPLIER = 31;

        public static bool IsEmpty(object[] array)
        {
            return array == null || array.Length == 0;
        }

        public static bool NullSafeEquals(object o1, object o2)
        {
            if (o1 == o2)
            {
                return true;
            }

            if (o1 == null || o2 == null)
            {
                return false;
            }

            if (o1.Equals(o2))
            {
                return true;
            }

            if (o1.GetType().IsArray && o2.GetType().IsArray)
            {
                return ArrayEquals(o1, o2);
            }

            return false;
        }

        public static int NullSafeHashCode(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            if (obj.GetType().IsArray)
            {
                return ArrayHashCode(obj);
            }

            return obj.GetHashCode();
        }

        // TODO: Add other .NET types
        internal static int ArrayHashCode(object o1)
        {
            if (o1 is object[])
            {
                return ArrayItemsHashCode<object>((object[])o1);
            }

            if (o1 is bool[])
            {
                return ArrayItemsHashCode<bool>((bool[])o1);
            }

            if (o1 is byte[])
            {
                return ArrayItemsHashCode<byte>((byte[])o1);
            }

            if (o1 is char[])
            {
                return ArrayItemsHashCode<char>((char[])o1);
            }

            if (o1 is double[])
            {
                return ArrayItemsHashCode<double>((double[])o1);
            }

            if (o1 is float[])
            {
                return ArrayItemsHashCode<float>((float[])o1);
            }

            if (o1 is int[])
            {
                return ArrayItemsHashCode<int>((int[])o1);
            }

            if (o1 is long[])
            {
                return ArrayItemsHashCode<long>((long[])o1);
            }

            if (o1 is short[])
            {
                return ArrayItemsHashCode<short>((short[])o1);
            }

            return 0;
        }

        // TODO: Add other .NET types
        internal static bool ArrayEquals(object o1, object o2)
        {
            if (o1 is object[] && o2 is object[])
            {
                return ArrayItemsEqual<object>((object[])o1, (object[])o2);
            }

            if (o1 is bool[] && o2 is bool[])
            {
                return ArrayItemsEqual<bool>((bool[])o1, (bool[])o2);
            }

            if (o1 is byte[] && o2 is byte[])
            {
                return ArrayItemsEqual<byte>((byte[])o1, (byte[])o2);
            }

            if (o1 is char[] && o2 is char[])
            {
                return ArrayItemsEqual<char>((char[])o1, (char[])o2);
            }

            if (o1 is double[] && o2 is double[])
            {
                return ArrayItemsEqual<double>((double[])o1, (double[])o2);
            }

            if (o1 is float[] && o2 is float[])
            {
                return ArrayItemsEqual<float>((float[])o1, (float[])o2);
            }

            if (o1 is int[] && o2 is int[])
            {
                return ArrayItemsEqual<int>((int[])o1, (int[])o2);
            }

            if (o1 is long[] && o2 is long[])
            {
                return ArrayItemsEqual<long>((long[])o1, (long[])o2);
            }

            if (o1 is short[] && o2 is short[])
            {
                return ArrayItemsEqual<short>((short[])o1, (short[])o2);
            }

            return false;
        }

        internal static bool ArrayItemsEqual<T>(T[] o1, T[] o2)
        {
            if (o1 == o2)
            {
                return true;
            }

            if (o1 == null || o2 == null)
            {
                return false;
            }

            if (o2.Length != o1.Length)
            {
                return false;
            }

            for (var i = 0; i < o1.Length; i++)
            {
                var item1 = o1[i];
                var item2 = o2[i];
                if (!(item1 == null ? item2 == null : item1.Equals(item2)))
                {
                    return false;
                }
            }

            return true;
        }

        internal static int ArrayItemsHashCode<T>(T[] array)
        {
            if (array == null)
            {
                return 0;
            }

            var hash = INITIAL_HASH;
            foreach (var element in array)
            {
                hash = (MULTIPLIER * hash) + NullSafeHashCode(element);
            }

            return hash;
        }
    }
}
