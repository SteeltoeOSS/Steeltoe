// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpNe : Operator
{
    public OpNe(int startPos, int endPos, params SpelNode[] operands)
        : base("!=", startPos, endPos, operands)
    {
        exitTypeDescriptor = TypeDescriptor.Z;
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        object leftValue = LeftOperand.GetValueInternal(state).Value;
        object rightValue = RightOperand.GetValueInternal(state).Value;
        leftActualDescriptor = CodeFlow.ToDescriptorFromObject(leftValue);
        rightActualDescriptor = CodeFlow.ToDescriptorFromObject(rightValue);
        return BooleanTypedValue.ForValue(!EqualityCheck(state.EvaluationContext, leftValue, rightValue));
    }

    // This check is different to the one in the other numeric operators (OpLt/etc)
    // because we allow simple object comparison
    public override bool IsCompilable()
    {
        SpelNode left = LeftOperand;
        SpelNode right = RightOperand;

        if (!left.IsCompilable() || !right.IsCompilable())
        {
            return false;
        }

        TypeDescriptor leftDesc = left.ExitDescriptor;
        TypeDescriptor rightDesc = right.ExitDescriptor;
        DescriptorComparison dc = DescriptorComparison.CheckNumericCompatibility(leftDesc, rightDesc, leftActualDescriptor, rightActualDescriptor);
        return !dc.AreNumbers || dc.AreCompatible;
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        CodeFlow.LoadEvaluationContext(gen);
        TypeDescriptor leftDesc = LeftOperand.ExitDescriptor;
        TypeDescriptor rightDesc = RightOperand.ExitDescriptor;
        bool leftPrim = CodeFlow.IsValueType(leftDesc);
        bool rightPrim = CodeFlow.IsValueType(rightDesc);

        cf.EnterCompilationScope();
        LeftOperand.GenerateCode(gen, cf);
        cf.ExitCompilationScope();

        if (leftPrim)
        {
            CodeFlow.InsertBoxIfNecessary(gen, leftDesc);
        }

        cf.EnterCompilationScope();
        RightOperand.GenerateCode(gen, cf);
        cf.ExitCompilationScope();

        if (rightPrim)
        {
            CodeFlow.InsertBoxIfNecessary(gen, rightDesc);
        }

        // returns bool
        gen.Emit(OpCodes.Call, EqualityCheckMethod);

        // Invert the boolean
        LocalBuilder result = gen.DeclareLocal(typeof(bool));
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Stloc, result);
        gen.Emit(OpCodes.Ldloc, result);

        cf.PushDescriptor(TypeDescriptor.Z);
    }
}
