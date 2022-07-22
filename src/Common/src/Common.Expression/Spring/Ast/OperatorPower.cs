// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
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
        var leftOp = LeftOperand;
        var rightOp = RightOperand;

        var leftOperand = leftOp.GetValueInternal(state).Value;
        var rightOperand = rightOp.GetValueInternal(state).Value;

        if (IsNumber(leftOperand) && IsNumber(rightOperand))
        {
            var leftConv = (IConvertible)leftOperand;
            var rightConv = (IConvertible)rightOperand;

            if (leftOperand is decimal)
            {
                var rightVal = rightConv.ToInt32(CultureInfo.InvariantCulture);
                var leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                var result = Math.Pow(leftVal, rightVal) as IConvertible;
                return new TypedValue(result.ToDecimal(CultureInfo.InvariantCulture));
            }
            else if (leftOperand is double || rightOperand is double)
            {
                var rightVal = rightConv.ToDouble(CultureInfo.InvariantCulture);
                var leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                return new TypedValue(Math.Pow(leftVal, rightVal));
            }
            else if (leftOperand is float || rightOperand is float)
            {
                var rightVal = rightConv.ToSingle(CultureInfo.InvariantCulture);
                var leftVal = leftConv.ToSingle(CultureInfo.InvariantCulture);
                return new TypedValue(Math.Pow(leftVal, rightVal));
            }

            var r = rightConv.ToDouble(CultureInfo.InvariantCulture);
            var l = leftConv.ToDouble(CultureInfo.InvariantCulture);

            var d = Math.Pow(l, r);
            var asLong = (long)d;
            if (asLong > int.MaxValue || leftOperand is long || rightOperand is long)
            {
                return new TypedValue(asLong);
            }
            else
            {
                return new TypedValue((int)asLong);
            }
        }

        return state.Operate(Operation.POWER, leftOperand, rightOperand);
    }
}