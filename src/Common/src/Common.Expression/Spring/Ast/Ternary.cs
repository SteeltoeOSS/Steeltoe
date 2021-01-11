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
    public class Ternary : SpelNode
    {
        public Ternary(int startPos, int endPos, params SpelNode[] args)
        : base(startPos, endPos, args)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            try
            {
                var value = _children[0].GetValue<bool>(state);
                var result = _children[value ? 1 : 2].GetValueInternal(state);
                ComputeExitTypeDescriptor();
                return result;
            }
            catch (Exception ex)
            {
                throw new SpelEvaluationException(GetChild(0).StartPosition, ex, SpelMessage.TYPE_CONVERSION_ERROR, "null", "System.Boolean");
            }
        }

        public override string ToStringAST()
        {
            return GetChild(0).ToStringAST() + " ? " + GetChild(1).ToStringAST() + " : " + GetChild(2).ToStringAST();
        }

        public override bool IsCompilable()
        {
            var condition = _children[0];
            var left = _children[1];
            var right = _children[2];
            return condition.IsCompilable() && left.IsCompilable() && right.IsCompilable() &&
                    CodeFlow.IsBooleanCompatible(condition.ExitDescriptor) &&
                    left.ExitDescriptor != null && right.ExitDescriptor != null;
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // // May reach here without it computed if all elements are literals
            //    computeExitTypeDescriptor();
            //    cf.enterCompilationScope();
            //    this.children[0].generateCode(mv, cf);
            //    String lastDesc = cf.lastDescriptor();
            //    Assert.state(lastDesc != null, "No last descriptor");
            //    if (!CodeFlow.isPrimitive(lastDesc))
            //    {
            //        CodeFlow.insertUnboxInsns(mv, 'Z', lastDesc);
            //    }
            //    cf.exitCompilationScope();
            //    Label elseTarget = new Label();
            //    Label endOfIf = new Label();
            //    mv.visitJumpInsn(IFEQ, elseTarget);
            //    cf.enterCompilationScope();
            //    this.children[1].generateCode(mv, cf);
            //    if (!CodeFlow.isPrimitive(this.exitTypeDescriptor))
            //    {
            //        lastDesc = cf.lastDescriptor();
            //        Assert.state(lastDesc != null, "No last descriptor");
            //        CodeFlow.insertBoxIfNecessary(mv, lastDesc.charAt(0));
            //    }
            //    cf.exitCompilationScope();
            //    mv.visitJumpInsn(GOTO, endOfIf);
            //    mv.visitLabel(elseTarget);
            //    cf.enterCompilationScope();
            //    this.children[2].generateCode(mv, cf);
            //    if (!CodeFlow.isPrimitive(this.exitTypeDescriptor))
            //    {
            //        lastDesc = cf.lastDescriptor();
            //        Assert.state(lastDesc != null, "No last descriptor");
            //        CodeFlow.insertBoxIfNecessary(mv, lastDesc.charAt(0));
            //    }
            //    cf.exitCompilationScope();
            //    mv.visitLabel(endOfIf);
            //    cf.pushDescriptor(this.exitTypeDescriptor);
        }

        private void ComputeExitTypeDescriptor()
        {
            if (_exitTypeDescriptor == null && _children[1].ExitDescriptor != null && _children[2].ExitDescriptor != null)
            {
                var leftDescriptor = _children[1].ExitDescriptor;
                var rightDescriptor = _children[2].ExitDescriptor;
                if (ObjectUtils.NullSafeEquals(leftDescriptor, rightDescriptor))
                {
                    _exitTypeDescriptor = leftDescriptor;
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
