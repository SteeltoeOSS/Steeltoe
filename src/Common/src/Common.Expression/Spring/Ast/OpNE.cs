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
    public class OpNE : Operator
    {
        public OpNE(int startPos, int endPos, params SpelNode[] operands)
        : base("!=", startPos, endPos, operands)
        {
            _exitTypeDescriptor = "Z";
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var leftValue = LeftOperand.GetValueInternal(state).Value;
            var rightValue = RightOperand.GetValueInternal(state).Value;
            _leftActualDescriptor = CodeFlow.ToDescriptorFromObject(leftValue);
            _rightActualDescriptor = CodeFlow.ToDescriptorFromObject(rightValue);
            return BooleanTypedValue.ForValue(!EqualityCheck(state.EvaluationContext, leftValue, rightValue));
        }

        // This check is different to the one in the other numeric operators (OpLt/etc)
        // because we allow simple object comparison
        public override bool IsCompilable()
        {
            var left = LeftOperand;
            var right = RightOperand;
            if (!left.IsCompilable() || !right.IsCompilable())
            {
                return false;
            }

            var leftDesc = left.ExitDescriptor;
            var rightDesc = right.ExitDescriptor;
            var dc = DescriptorComparison.CheckNumericCompatibility(leftDesc, rightDesc, _leftActualDescriptor, _rightActualDescriptor);
            return !dc.AreNumbers || dc.AreCompatible;
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // cf.loadEvaluationContext(mv);
            //    String leftDesc = getLeftOperand().exitTypeDescriptor;
            //    String rightDesc = getRightOperand().exitTypeDescriptor;
            //    boolean leftPrim = CodeFlow.isPrimitive(leftDesc);
            //    boolean rightPrim = CodeFlow.isPrimitive(rightDesc);

            // cf.enterCompilationScope();
            //    getLeftOperand().generateCode(mv, cf);
            //    cf.exitCompilationScope();
            //    if (leftPrim)
            //    {
            //        CodeFlow.insertBoxIfNecessary(mv, leftDesc.charAt(0));
            //    }
            //    cf.enterCompilationScope();
            //    getRightOperand().generateCode(mv, cf);
            //    cf.exitCompilationScope();
            //    if (rightPrim)
            //    {
            //        CodeFlow.insertBoxIfNecessary(mv, rightDesc.charAt(0));
            //    }

            // String operatorClassName = typeof(Operator).getName().replace('.', '/');
            //    String evaluationContextClassName = typeof(EvaluationContext).getName().replace('.', '/');
            //    mv.visitMethodInsn(INVOKESTATIC, operatorClassName, "equalityCheck",
            //            "(L" + evaluationContextClassName + ";Ljava/lang/Object;Ljava/lang/Object;)Z", false);

            // // Invert the boolean
            //    Label notZero = new Label();
            //    Label end = new Label();
            //    mv.visitJumpInsn(IFNE, notZero);
            //    mv.visitInsn(ICONST_1);
            //    mv.visitJumpInsn(GOTO, end);
            //    mv.visitLabel(notZero);
            //    mv.visitInsn(ICONST_0);
            //    mv.visitLabel(end);

            // cf.pushDescriptor("Z");
        }
    }
}
