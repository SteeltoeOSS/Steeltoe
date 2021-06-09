// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System.Collections;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class OperatorBetween : Operator
    {
        public OperatorBetween(int startPos, int endPos, params SpelNode[] operands)
            : base("between", startPos, endPos, operands)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var left = LeftOperand.GetValueInternal(state).Value;
            var right = RightOperand.GetValueInternal(state).Value;
            if (!(right is IList) || ((IList)right).Count != 2)
            {
                throw new SpelEvaluationException(RightOperand.StartPosition, SpelMessage.BETWEEN_RIGHT_OPERAND_MUST_BE_TWO_ELEMENT_LIST);
            }

            var list = (IList)right;
            var low = list[0];
            var high = list[1];
            var comp = state.TypeComparator;
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
}
