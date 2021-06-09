// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class OpOr : Operator
    {
        public OpOr(int startPos, int endPos, params SpelNode[] operands)
        : base("or", startPos, endPos, operands)
        {
            _exitTypeDescriptor = TypeDescriptor.Z;
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            if (GetBooleanValue(state, LeftOperand))
            {
                // no need to evaluate right operand
                return BooleanTypedValue.TRUE;
            }

            return BooleanTypedValue.ForValue(GetBooleanValue(state, RightOperand));
        }

        // private void AssertValueNotNull(bool value)
        // {
        //    if (value == null)
        //    {
        //        throw new SpelEvaluationException(SpelMessage.TYPE_CONVERSION_ERROR, "null", "System.Boolean");
        //    }
        // }
        public override bool IsCompilable()
        {
            var left = LeftOperand;
            var right = RightOperand;
            return left.IsCompilable() && right.IsCompilable() &&
                    CodeFlow.IsBooleanCompatible(left.ExitDescriptor) &&
                    CodeFlow.IsBooleanCompatible(right.ExitDescriptor);
        }

        public override void GenerateCode(ILGenerator gen, CodeFlow cf)
        {
            // pseudo: if (leftOperandValue) { result=true; } else { result=rightOperandValue; }
            var elseTarget = gen.DefineLabel();
            var endIfTarget = gen.DefineLabel();
            var result = gen.DeclareLocal(typeof(bool));

            cf.EnterCompilationScope();
            LeftOperand.GenerateCode(gen, cf);
            cf.UnboxBooleanIfNecessary(gen);
            cf.ExitCompilationScope();
            gen.Emit(OpCodes.Brfalse, elseTarget);
            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Stloc, result);
            gen.Emit(OpCodes.Br, endIfTarget);
            gen.MarkLabel(elseTarget);
            cf.EnterCompilationScope();
            RightOperand.GenerateCode(gen, cf);
            cf.UnboxBooleanIfNecessary(gen);
            cf.ExitCompilationScope();
            gen.Emit(OpCodes.Stloc, result);
            gen.MarkLabel(endIfTarget);
            gen.Emit(OpCodes.Ldloc, result);
            cf.PushDescriptor(_exitTypeDescriptor);
        }

        private bool GetBooleanValue(ExpressionState state, SpelNode operand)
        {
            try
            {
                var value = operand.GetValue<bool>(state);
                return value;
            }
            catch (SpelEvaluationException ee)
            {
                ee.Position = operand.StartPosition;
                throw;
            }
        }
    }
}
