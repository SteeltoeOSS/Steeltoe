// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
            if (o1 is object[] v)
            {
                return ArrayItemsHashCode<object>(v);
            }

            if (o1 is bool[] v1)
            {
                return ArrayItemsHashCode<bool>(v1);
            }

            if (o1 is byte[] v2)
            {
                return ArrayItemsHashCode<byte>(v2);
            }

            if (o1 is char[] v3)
            {
                return ArrayItemsHashCode<char>(v3);
            }

            if (o1 is double[] v4)
            {
                return ArrayItemsHashCode<double>(v4);
            }

            if (o1 is float[] v5)
            {
                return ArrayItemsHashCode<float>(v5);
            }

            if (o1 is int[] v6)
            {
                return ArrayItemsHashCode<int>(v6);
            }

            if (o1 is long[] v7)
            {
                return ArrayItemsHashCode<long>(v7);
            }

            if (o1 is short[] v8)
            {
                return ArrayItemsHashCode<short>(v8);
            }

            return 0;
        }

        // TODO: Add other .NET types
        internal static bool ArrayEquals(object o1, object o2)
        {
            if (o1 is object[] v && o2 is object[] v1)
            {
                return ArrayItemsEqual<object>(v, v1);
            }

            if (o1 is bool[] v2 && o2 is bool[] v3)
            {
                return ArrayItemsEqual<bool>(v2, v3);
            }

            if (o1 is byte[] v4 && o2 is byte[] v5)
            {
                return ArrayItemsEqual<byte>(v4, v5);
            }

            if (o1 is char[] v6 && o2 is char[] v7)
            {
                return ArrayItemsEqual<char>(v6, v7);
            }

            if (o1 is double[] v8 && o2 is double[] v9)
            {
                return ArrayItemsEqual<double>(v8, v9);
            }

            if (o1 is float[] v10 && o2 is float[] v11)
            {
                return ArrayItemsEqual<float>(v10, v11);
            }

            if (o1 is int[] v12 && o2 is int[] v13)
            {
                return ArrayItemsEqual<int>(v12, v13);
            }

            if (o1 is long[] v14 && o2 is long[] v15)
            {
                return ArrayItemsEqual<long>(v14, v15);
            }

            if (o1 is short[] v16 && o2 is short[] v17)
            {
                return ArrayItemsEqual<short>(v16, v17);
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
