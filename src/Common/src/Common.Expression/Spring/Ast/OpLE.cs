// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Globalization;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpLE : Operator
{
    public OpLE(int startPos, int endPos, params SpelNode[] operands)
        : base("<=", startPos, endPos, operands)
    {
        _exitTypeDescriptor = TypeDescriptor.Z;
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        var left = LeftOperand.GetValueInternal(state).Value;
        var right = RightOperand.GetValueInternal(state).Value;

        _leftActualDescriptor = CodeFlow.ToDescriptorFromObject(left);
        _rightActualDescriptor = CodeFlow.ToDescriptorFromObject(right);

        if (IsNumber(left) && IsNumber(right))
        {
            var leftConv = (IConvertible)left;
            var rightConv = (IConvertible)right;

            if (left is decimal || right is decimal)
            {
                var leftVal = leftConv.ToDecimal(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToDecimal(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal.CompareTo(rightVal) <= 0);
            }
            else if (left is double || right is double)
            {
                var leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToDouble(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal <= rightVal);
            }
            else if (left is float || right is float)
            {
                var leftVal = leftConv.ToSingle(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToSingle(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal <= rightVal);
            }
            else if (left is long || right is long)
            {
                var leftVal = leftConv.ToInt64(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToInt64(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal <= rightVal);
            }
            else if (left is int || right is int)
            {
                var leftVal = leftConv.ToInt32(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToInt32(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal <= rightVal);
            }
            else if (left is short || right is short)
            {
                var leftVal = leftConv.ToInt16(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToInt16(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal <= rightVal);
            }
            else if (left is byte || right is byte)
            {
                var leftVal = leftConv.ToByte(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToByte(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal <= rightVal);
            }
            else if (left is ulong || right is ulong)
            {
                var leftVal = leftConv.ToUInt64(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToUInt64(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal <= rightVal);
            }
            else if (left is uint || right is uint)
            {
                var leftVal = leftConv.ToUInt32(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToUInt32(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal <= rightVal);
            }
            else if (left is ushort || right is ushort)
            {
                var leftVal = leftConv.ToUInt16(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToUInt16(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal <= rightVal);
            }
            else if (left is sbyte || right is sbyte)
            {
                var leftVal = leftConv.ToSByte(CultureInfo.InvariantCulture);
                var rightVal = rightConv.ToSByte(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal <= rightVal);
            }
        }

        return BooleanTypedValue.ForValue(state.TypeComparator.Compare(left, right) <= 0);
    }

    public override bool IsCompilable()
    {
        return IsCompilableOperatorUsingNumerics();
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        GenerateComparisonCode(gen, cf, OpCodes.Bgt);
    }
}
