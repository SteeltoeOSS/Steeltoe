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
    public class OperatorNot : SpelNode
    {
        public OperatorNot(int startPos, int endPos, SpelNode operand)
        : base(startPos, endPos, operand)
        {
            _exitTypeDescriptor = "Z";
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            try
            {
                var value = _children[0].GetValue<bool>(state);
                return BooleanTypedValue.ForValue(!value);
            }
            catch (SpelEvaluationException ex)
            {
                ex.Position = GetChild(0).StartPosition;
                throw;
            }
            catch (Exception ex)
            {
                throw new SpelEvaluationException(SpelMessage.TYPE_CONVERSION_ERROR, ex, "null", "System.Boolean");
            }
        }

        public override string ToStringAST()
        {
            return "!" + GetChild(0).ToStringAST();
        }

        public override bool IsCompilable()
        {
            var child = _children[0];
            return child.IsCompilable() && CodeFlow.IsBooleanCompatible(child.ExitDescriptor);
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // this.children[0].generateCode(mv, cf);
            //    cf.unboxBooleanIfNecessary(mv);
            //    Label elseTarget = new Label();
            //    Label endOfIf = new Label();
            //    mv.visitJumpInsn(IFNE, elseTarget);
            //    mv.visitInsn(ICONST_1); // TRUE
            //    mv.visitJumpInsn(GOTO, endOfIf);
            //    mv.visitLabel(elseTarget);
            //    mv.visitInsn(ICONST_0); // FALSE
            //    mv.visitLabel(endOfIf);
            //    cf.pushDescriptor(this.exitTypeDescriptor);
        }
    }
}
