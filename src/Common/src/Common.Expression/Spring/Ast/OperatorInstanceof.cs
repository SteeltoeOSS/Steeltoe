// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Support;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class OperatorInstanceof : Operator
    {
        private Type _type;

        public OperatorInstanceof(int startPos, int endPos, params SpelNode[] operands)
            : base("instanceof", startPos, endPos, operands)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var rightOperand = RightOperand;
            var left = LeftOperand.GetValueInternal(state);
            var right = rightOperand.GetValueInternal(state);
            var leftValue = left.Value;
            var rightValue = right.Value;
            BooleanTypedValue result;
            if (!(rightValue is Type))
            {
                throw new SpelEvaluationException(RightOperand.StartPosition, SpelMessage.INSTANCEOF_OPERATOR_NEEDS_CLASS_OPERAND, rightValue == null ? "null" : rightValue.GetType().FullName);
            }

            var rightClass = (Type)rightValue;
            if (leftValue == null)
            {
                result = BooleanTypedValue.FALSE;  // null is not an instanceof anything
            }
            else
            {
                result = BooleanTypedValue.ForValue(rightClass.IsAssignableFrom(leftValue.GetType()));
            }

            _type = rightClass;
            if (rightOperand is TypeReference)
            {
                // Can only generate bytecode where the right operand is a direct type reference,
                // not if it is indirect (for example when right operand is a variable reference)
                _exitTypeDescriptor = "Z";
            }

            return result;
        }

        public override bool IsCompilable()
        {
            return _exitTypeDescriptor != null && LeftOperand.IsCompilable();
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // getLeftOperand().generateCode(mv, cf);
            //    CodeFlow.insertBoxIfNecessary(mv, cf.lastDescriptor());
            //    Assert.state(this.type != null, "No type available");
            //    if (this.type.isPrimitive())
            //    {
            //        // always false - but left operand code always driven
            //        // in case it had side effects
            //        mv.visitInsn(POP);
            //        mv.visitInsn(ICONST_0); // value of false
            //    }
            //    else
            //    {
            //        mv.visitTypeInsn(INSTANCEOF, Type.getInternalName(this.type));
            //    }
            //    cf.pushDescriptor(this.exitTypeDescriptor);
        }
    }
}
