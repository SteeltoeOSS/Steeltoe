// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpOr : Operator
{
    public OpOr(int startPos, int endPos, params SpelNode[] operands)
        : base("or", startPos, endPos, operands)
    {
        exitTypeDescriptor = TypeDescriptor.Z;
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        if (GetBooleanValue(state, LeftOperand))
        {
            // no need to evaluate right operand
            return BooleanTypedValue.True;
        }

        return BooleanTypedValue.ForValue(GetBooleanValue(state, RightOperand));
    }

    public override bool IsCompilable()
    {
        SpelNode left = LeftOperand;
        SpelNode right = RightOperand;

        return left.IsCompilable() && right.IsCompilable() && CodeFlow.IsBooleanCompatible(left.ExitDescriptor) &&
            CodeFlow.IsBooleanCompatible(right.ExitDescriptor);
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        // pseudo: if (leftOperandValue) { result=true; } else { result=rightOperandValue; }
        Label elseTarget = gen.DefineLabel();
        Label endIfTarget = gen.DefineLabel();
        LocalBuilder result = gen.DeclareLocal(typeof(bool));

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
        cf.PushDescriptor(exitTypeDescriptor);
    }

    private bool GetBooleanValue(ExpressionState state, SpelNode operand)
    {
        try
        {
            bool value = operand.GetValue<bool>(state);
            return value;
        }
        catch (SpelEvaluationException ee)
        {
            ee.Position = operand.StartPosition;
            throw;
        }
    }
}
