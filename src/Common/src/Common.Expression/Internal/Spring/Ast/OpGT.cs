// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection.Emit;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpGt : Operator
{
    public OpGt(int startPos, int endPos, params SpelNode[] operands)
        : base(">", startPos, endPos, operands)
    {
        exitTypeDescriptor = TypeDescriptor.Z;
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        object left = LeftOperand.GetValueInternal(state).Value;
        object right = RightOperand.GetValueInternal(state).Value;

        leftActualDescriptor = CodeFlow.ToDescriptorFromObject(left);
        rightActualDescriptor = CodeFlow.ToDescriptorFromObject(right);

        if (IsNumber(left) && IsNumber(right))
        {
            var leftConv = (IConvertible)left;
            var rightConv = (IConvertible)right;

            if (left is decimal || right is decimal)
            {
                decimal leftVal = leftConv.ToDecimal(CultureInfo.InvariantCulture);
                decimal rightVal = rightConv.ToDecimal(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal.CompareTo(rightVal) > 0);
            }

            if (left is double || right is double)
            {
                double leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                double rightVal = rightConv.ToDouble(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal > rightVal);
            }

            if (left is float || right is float)
            {
                float leftVal = leftConv.ToSingle(CultureInfo.InvariantCulture);
                float rightVal = rightConv.ToSingle(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal > rightVal);
            }

            if (left is long || right is long)
            {
                long leftVal = leftConv.ToInt64(CultureInfo.InvariantCulture);
                long rightVal = rightConv.ToInt64(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal > rightVal);
            }

            if (left is int || right is int)
            {
                int leftVal = leftConv.ToInt32(CultureInfo.InvariantCulture);
                int rightVal = rightConv.ToInt32(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal > rightVal);
            }

            if (left is short || right is short)
            {
                short leftVal = leftConv.ToInt16(CultureInfo.InvariantCulture);
                short rightVal = rightConv.ToInt16(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal > rightVal);
            }

            if (left is byte || right is byte)
            {
                byte leftVal = leftConv.ToByte(CultureInfo.InvariantCulture);
                byte rightVal = rightConv.ToByte(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal > rightVal);
            }

            if (left is ulong || right is ulong)
            {
                ulong leftVal = leftConv.ToUInt64(CultureInfo.InvariantCulture);
                ulong rightVal = rightConv.ToUInt64(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal > rightVal);
            }

            if (left is uint || right is uint)
            {
                uint leftVal = leftConv.ToUInt32(CultureInfo.InvariantCulture);
                uint rightVal = rightConv.ToUInt32(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal > rightVal);
            }

            if (left is ushort || right is ushort)
            {
                ushort leftVal = leftConv.ToUInt16(CultureInfo.InvariantCulture);
                ushort rightVal = rightConv.ToUInt16(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal > rightVal);
            }

            if (left is sbyte || right is sbyte)
            {
                sbyte leftVal = leftConv.ToSByte(CultureInfo.InvariantCulture);
                sbyte rightVal = rightConv.ToSByte(CultureInfo.InvariantCulture);
                return BooleanTypedValue.ForValue(leftVal > rightVal);
            }
        }

        return BooleanTypedValue.ForValue(state.TypeComparator.Compare(left, right) > 0);
    }

    public override bool IsCompilable()
    {
        return IsCompilableOperatorUsingNumerics();
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        GenerateComparisonCode(gen, cf, OpCodes.Ble);
    }
}
