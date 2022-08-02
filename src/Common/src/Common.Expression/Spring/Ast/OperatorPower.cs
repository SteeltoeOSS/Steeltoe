// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OperatorPower : Operator
{
    public OperatorPower(int startPos, int endPos, params SpelNode[] operands)
        : base("^", startPos, endPos, operands)
    {
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        SpelNode leftOp = LeftOperand;
        SpelNode rightOp = RightOperand;

        object leftOperand = leftOp.GetValueInternal(state).Value;
        object rightOperand = rightOp.GetValueInternal(state).Value;

        if (IsNumber(leftOperand) && IsNumber(rightOperand))
        {
            var leftConv = (IConvertible)leftOperand;
            var rightConv = (IConvertible)rightOperand;

            if (leftOperand is decimal)
            {
                int rightVal = rightConv.ToInt32(CultureInfo.InvariantCulture);
                double leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                var result = Math.Pow(leftVal, rightVal) as IConvertible;
                return new TypedValue(result.ToDecimal(CultureInfo.InvariantCulture));
            }

            if (leftOperand is double || rightOperand is double)
            {
                double rightVal = rightConv.ToDouble(CultureInfo.InvariantCulture);
                double leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                return new TypedValue(Math.Pow(leftVal, rightVal));
            }

            if (leftOperand is float || rightOperand is float)
            {
                float rightVal = rightConv.ToSingle(CultureInfo.InvariantCulture);
                float leftVal = leftConv.ToSingle(CultureInfo.InvariantCulture);
                return new TypedValue(Math.Pow(leftVal, rightVal));
            }

            double r = rightConv.ToDouble(CultureInfo.InvariantCulture);
            double l = leftConv.ToDouble(CultureInfo.InvariantCulture);

            double d = Math.Pow(l, r);
            long asLong = (long)d;

            if (asLong > int.MaxValue || leftOperand is long || rightOperand is long)
            {
                return new TypedValue(asLong);
            }

            return new TypedValue((int)asLong);
        }

        return state.Operate(Operation.Power, leftOperand, rightOperand);
    }
}
