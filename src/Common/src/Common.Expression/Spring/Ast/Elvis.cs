// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class Elvis : SpelNode
    {
        public Elvis(int startPos, int endPos, params SpelNode[] args)
        : base(startPos, endPos, args)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var value = _children[0].GetValueInternal(state);

            // If this check is changed, the generateCode method will need changing too
            if (!(value.Value == null || string.Empty.Equals(value.Value)))
            {
                return value;
            }
            else
            {
                var result = _children[1].GetValueInternal(state);
                ComputeExitTypeDescriptor();
                return result;
            }
        }

        public override string ToStringAST()
        {
            return GetChild(0).ToStringAST() + " ?: " + GetChild(1).ToStringAST();
        }

        public override bool IsCompilable()
        {
            var condition = _children[0];
            var ifNullValue = _children[1];
            return condition.IsCompilable() && ifNullValue.IsCompilable() &&
                    condition.ExitDescriptor != null && ifNullValue.ExitDescriptor != null;
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            //// exit type descriptor can be null if both components are literal expressions
            // computeExitTypeDescriptor();
            // cf.enterCompilationScope();
            // this.children[0].generateCode(mv, cf);
            // String lastDesc = cf.lastDescriptor();
            // Assert.state(lastDesc != null, "No last descriptor");
            // CodeFlow.insertBoxIfNecessary(mv, lastDesc.charAt(0));
            // cf.exitCompilationScope();
            // Label elseTarget = new Label();
            // Label endOfIf = new Label();
            // mv.visitInsn(DUP);
            // mv.visitJumpInsn(IFNULL, elseTarget);
            //// Also check if empty string, as per the code in the interpreted version
            // mv.visitInsn(DUP);
            // mv.visitLdcInsn("");
            // mv.visitInsn(SWAP);
            // mv.visitMethodInsn(INVOKEVIRTUAL, "java/lang/String", "equals", "(Ljava/lang/Object;)Z", false);
            // mv.visitJumpInsn(IFEQ, endOfIf);  // if not empty, drop through to elseTarget
            // mv.visitLabel(elseTarget);
            // mv.visitInsn(POP);
            // cf.enterCompilationScope();
            // this.children[1].generateCode(mv, cf);
            // if (!CodeFlow.isPrimitive(this.exitTypeDescriptor))
            // {
            //    lastDesc = cf.lastDescriptor();
            //    Assert.state(lastDesc != null, "No last descriptor");
            //    CodeFlow.insertBoxIfNecessary(mv, lastDesc.charAt(0));
            // }
            // cf.exitCompilationScope();
            // mv.visitLabel(endOfIf);
            // cf.pushDescriptor(this.exitTypeDescriptor);
        }

        private void ComputeExitTypeDescriptor()
        {
            if (_exitTypeDescriptor == null && _children[0].ExitDescriptor != null && _children[1].ExitDescriptor != null)
            {
                var conditionDescriptor = _children[0].ExitDescriptor;
                var ifNullValueDescriptor = _children[1].ExitDescriptor;
                if (ObjectUtils.NullSafeEquals(conditionDescriptor, ifNullValueDescriptor))
                {
                    _exitTypeDescriptor = conditionDescriptor;
                }
                else
                {
                    // Use the easiest to compute common super type
                    _exitTypeDescriptor = "LSystem/Object";
                }
            }
        }
    }
}
