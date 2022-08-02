// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OperatorBetween : Operator
{
    public OperatorBetween(int startPos, int endPos, params SpelNode[] operands)
        : base("between", startPos, endPos, operands)
    {
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        object left = LeftOperand.GetValueInternal(state).Value;
        object right = RightOperand.GetValueInternal(state).Value;

        if (right is not IList list || list.Count != 2)
        {
            throw new SpelEvaluationException(RightOperand.StartPosition, SpelMessage.BetweenRightOperandMustBeTwoElementList);
        }

        object low = list[0];
        object high = list[1];
        ITypeComparator comp = state.TypeComparator;

        try
        {
            return BooleanTypedValue.ForValue(comp.Compare(left, low) >= 0 && comp.Compare(left, high) <= 0);
        }
        catch (SpelEvaluationException ex)
        {
            ex.Position = StartPosition;
            throw;
        }
    }
}
