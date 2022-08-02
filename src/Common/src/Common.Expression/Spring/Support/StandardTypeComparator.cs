// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class StandardTypeComparator : ITypeComparator
{
    public bool CanCompare(object firstObject, object secondObject)
    {
        if (firstObject == null || secondObject == null)
        {
            return true;
        }

        // if (firstObject is Number && secondObject is Number)
        // {
        //    return true;
        // }
        if (firstObject is IComparable)
        {
            return true;
        }

        return false;
    }

    public int Compare(object firstObject, object secondObject)
    {
        // If one is null, check if the other is
        if (firstObject == null)
        {
            return secondObject == null ? 0 : -1;
        }

        if (secondObject == null)
        {
            return 1; // firstObject cannot be null at this point
        }

        if (firstObject is decimal || secondObject is decimal)
        {
            decimal leftNum = Convert.ToDecimal(firstObject);
            decimal rightNum = Convert.ToDecimal(secondObject);
            return leftNum.CompareTo(rightNum);
        }

        if (firstObject is double || secondObject is double)
        {
            double leftNum = Convert.ToDouble(firstObject);
            double rightNum = Convert.ToDouble(secondObject);
            return leftNum.CompareTo(rightNum);
        }

        if (firstObject is float || secondObject is float)
        {
            float leftNum = Convert.ToSingle(firstObject);
            float rightNum = Convert.ToSingle(secondObject);
            return leftNum.CompareTo(rightNum);
        }

        if (firstObject is long || secondObject is long)
        {
            long leftNum = Convert.ToInt64(firstObject);
            long rightNum = Convert.ToInt64(secondObject);
            return leftNum.CompareTo(rightNum);
        }

        // else if (leftNumber is Integer || rightNumber is Integer)
        //    {
        //        return Integer.compare(leftNumber.intValue(), rightNumber.intValue());
        //    }

        // else if (leftNumber is Short || rightNumber is Short)
        //    {
        //        return Short.compare(leftNumber.shortValue(), rightNumber.shortValue());
        //    }

        // else if (leftNumber is Byte || rightNumber is Byte)
        //    {
        //        return Byte.compare(leftNumber.byteValue(), rightNumber.byteValue());
        //    }

        // else
        //    {
        //        // Unknown Number subtypes -> best guess is double multiplication
        //        return Double.compare(leftNumber.doubleValue(), rightNumber.doubleValue());
        //    }
        // }
        try
        {
            if (firstObject is IComparable comparable)
            {
                return comparable.CompareTo(secondObject);
            }
        }
        catch (Exception ex)
        {
            throw new SpelEvaluationException(ex, SpelMessage.NotComparable, firstObject.GetType(), secondObject.GetType());
        }

        throw new SpelEvaluationException(SpelMessage.NotComparable, firstObject.GetType(), secondObject.GetType());
    }
}
