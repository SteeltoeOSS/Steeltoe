// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.TestResources;

public class ArrayContainer
{
    public int[] Ints = new int[3];
    public long[] Longs = new long[3];
    public double[] Doubles = new double[3];
    public byte[] Bytes = new byte[3];
    public char[] Chars = new char[3];
    public short[] Shorts = new short[3];
    public bool[] Booleans = new bool[3];
    public float[] Floats = new float[3];

    public ArrayContainer()
    {
        // setup some values
        Ints[0] = 42;
        Longs[0] = 42L;
        Doubles[0] = 42.0d;
        Bytes[0] = 42;
        Chars[0] = (char)42;
        Shorts[0] = 42;
        Booleans[0] = true;
        Floats[0] = 42.0f;
    }
}