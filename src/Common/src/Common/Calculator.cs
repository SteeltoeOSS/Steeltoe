// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common;

public sealed class Calculator
{
    public int Add(int left, int right)
    {
        if (left < 0 || right < 0)
        {
            throw new ArgumentException("Only non-negative integers are allowed.");
        }

        return left + right;
    }

    public int Subtract(int left, int right)
    {
        return left - right;
    }
}
