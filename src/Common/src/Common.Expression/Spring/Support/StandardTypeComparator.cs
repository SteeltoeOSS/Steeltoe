// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Support
{
    public class StandardTypeComparator : ITypeComparator
    {
        public bool CanCompare(object left, object right)
        {
            if (left == null || right == null)
            {
                return true;
            }

            // if (left is Number && right is Number)
            // {
            //    return true;
            // }
            if (left is IComparable)
            {
                return true;
            }

            return false;
        }

        public int Compare(object left, object right)
        {
            // If one is null, check if the other is
            if (left == null)
            {
                return right == null ? 0 : -1;
            }
            else if (right == null)
            {
                return 1;  // left cannot be null at this point
            }

            if (left is decimal || right is decimal)
            {
                var leftNum = Convert.ToDecimal(left);
                var rightNum = Convert.ToDecimal(right);
                return leftNum.CompareTo(rightNum);
            }
            else if (left is double || right is double)
            {
                var leftNum = Convert.ToDouble(left);
                var rightNum = Convert.ToDouble(right);
                return leftNum.CompareTo(rightNum);
            }
            else if (left is float || right is float)
            {
                var leftNum = Convert.ToSingle(left);
                var rightNum = Convert.ToSingle(right);
                return leftNum.CompareTo(rightNum);
            }
            else if (left is long || right is long)
            {
                var leftNum = Convert.ToInt64(left);
                var rightNum = Convert.ToInt64(right);
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
                if (left is IComparable)
                {
                    return ((IComparable)left).CompareTo(right);
                }
            }
            catch (Exception ex)
            {
                throw new SpelEvaluationException(ex, SpelMessage.NOT_COMPARABLE, left.GetType(), right.GetType());
            }

            throw new SpelEvaluationException(SpelMessage.NOT_COMPARABLE, left.GetType(), right.GetType());
        }
    }
}
