// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpNE : Operator
{
    public OpNE(int startPos, int endPos, params SpelNode[] operands)
        : base("!=", startPos, endPos, operands)
    {
        _exitTypeDescriptor = TypeDescriptor.Z;
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

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        CodeFlow.LoadEvaluationContext(gen);
        var leftDesc = LeftOperand.ExitDescriptor;
        var rightDesc = RightOperand.ExitDescriptor;
        var leftPrim = CodeFlow.IsValueType(leftDesc);
        var rightPrim = CodeFlow.IsValueType(rightDesc);

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
        gen.Emit(OpCodes.Call, _equalityCheck);

        // Invert the boolean
        var result = gen.DeclareLocal(typeof(bool));
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ceq);
        gen.Emit(OpCodes.Stloc, result);
        gen.Emit(OpCodes.Ldloc, result);

        cf.PushDescriptor(TypeDescriptor.Z);
    }
}
