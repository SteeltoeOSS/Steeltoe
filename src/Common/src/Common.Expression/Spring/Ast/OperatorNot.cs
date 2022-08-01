// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System.Reflection.Emit;

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
            var value = children[0].GetValue<bool>(state);
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
        var child = children[0];
        return child.IsCompilable() && CodeFlow.IsBooleanCompatible(child.ExitDescriptor);
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        var elseTarget = gen.DefineLabel();
        var endIfTarget = gen.DefineLabel();
        var result = gen.DeclareLocal(typeof(bool));

        var child = children[0];
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
