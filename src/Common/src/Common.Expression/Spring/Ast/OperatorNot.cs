// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OperatorNot : SpelNode
{
    public OperatorNot(int startPos, int endPos, SpelNode operand)
        : base(startPos, endPos, operand)
    {
        exitTypeDescriptor = TypeDescriptor.Z;
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        try
        {
            bool value = children[0].GetValue<bool>(state);
            return BooleanTypedValue.ForValue(!value);
        }
        catch (SpelEvaluationException ex)
        {
            ex.Position = GetChild(0).StartPosition;
            throw;
        }
        catch (Exception ex)
        {
            throw new SpelEvaluationException(SpelMessage.TypeConversionError, ex, "null", "System.Boolean");
        }
    }

    public override string ToStringAst()
    {
        return $"!{GetChild(0).ToStringAst()}";
    }

    public override bool IsCompilable()
    {
        SpelNode child = children[0];
        return child.IsCompilable() && CodeFlow.IsBooleanCompatible(child.ExitDescriptor);
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        Label elseTarget = gen.DefineLabel();
        Label endIfTarget = gen.DefineLabel();
        LocalBuilder result = gen.DeclareLocal(typeof(bool));

        SpelNode child = children[0];
        child.GenerateCode(gen, cf);
        cf.UnboxBooleanIfNecessary(gen);
        gen.Emit(OpCodes.Brtrue, elseTarget);
        gen.Emit(OpCodes.Ldc_I4_1);
        gen.Emit(OpCodes.Stloc, result);
        gen.Emit(OpCodes.Br, endIfTarget);
        gen.MarkLabel(elseTarget);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Stloc, result);
        gen.MarkLabel(endIfTarget);
        gen.Emit(OpCodes.Ldloc, result);
        cf.PushDescriptor(exitTypeDescriptor);
    }
}
