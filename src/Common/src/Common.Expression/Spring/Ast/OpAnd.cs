// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class OpAnd : Operator
    {
        public OpAnd(int startPos, int endPos, params SpelNode[] operands)
            : base("and", startPos, endPos, operands)
        {
            _exitTypeDescriptor = "Z";
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            if (!GetBooleanValue(state, LeftOperand))
            {
                // no need to evaluate right operand
                return BooleanTypedValue.FALSE;
            }

            return BooleanTypedValue.ForValue(GetBooleanValue(state, RightOperand));
        }

        public override bool IsCompilable()
        {
            var left = LeftOperand;
            var right = RightOperand;
            return left.IsCompilable() && right.IsCompilable() &&
                    CodeFlow.IsBooleanCompatible(left.ExitDescriptor) &&
                    CodeFlow.IsBooleanCompatible(right.ExitDescriptor);
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // // Pseudo: if (!leftOperandValue) { result=false; } else { result=rightOperandValue; }
            //    Label elseTarget = new Label();
            //    Label endOfIf = new Label();
            //    cf.enterCompilationScope();
            //    getLeftOperand().generateCode(mv, cf);
            //    cf.unboxBooleanIfNecessary(mv);
            //    cf.exitCompilationScope();
            //    mv.visitJumpInsn(IFNE, elseTarget);
            //    mv.visitLdcInsn(0); // FALSE
            //    mv.visitJumpInsn(GOTO, endOfIf);
            //    mv.visitLabel(elseTarget);
            //    cf.enterCompilationScope();
            //    getRightOperand().generateCode(mv, cf);
            //    cf.unboxBooleanIfNecessary(mv);
            //    cf.exitCompilationScope();
            //    mv.visitLabel(endOfIf);
            //    cf.pushDescriptor(this.exitTypeDescriptor);
        }

        private bool GetBooleanValue(ExpressionState state, SpelNode operand)
        {
            try
            {
                var value = operand.GetValue<bool>(state);

                // AssertValueNotNull(value);
                return value;
            }
            catch (SpelEvaluationException ex)
            {
                ex.Position = operand.StartPosition;
                throw;
            }
        }

        // private void AssertValueNotNull(bool value)
        // {
        //    if (value == null)
        //    {
        //        throw new SpelEvaluationException(SpelMessage.TYPE_CONVERSION_ERROR, "null", "System.Boolean");
        //    }
        // }
    }
}
