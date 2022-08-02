// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;
using Steeltoe.Common.Util;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

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
            bool value = children[0].GetValue<bool>(state);
            ITypedValue result = children[value ? 1 : 2].GetValueInternal(state);
            ComputeExitTypeDescriptor();
            return result;
        }
        catch (Exception ex)
        {
            throw new SpelEvaluationException(GetChild(0).StartPosition, ex, SpelMessage.TypeConversionError, "null", "System.Boolean");
        }
    }

    public override string ToStringAst()
    {
        return $"{GetChild(0).ToStringAst()} ? {GetChild(1).ToStringAst()} : {GetChild(2).ToStringAst()}";
    }

    public override bool IsCompilable()
    {
        SpelNode condition = children[0];
        SpelNode left = children[1];
        SpelNode right = children[2];

        return condition.IsCompilable() && left.IsCompilable() && right.IsCompilable() && CodeFlow.IsBooleanCompatible(condition.ExitDescriptor) &&
            left.ExitDescriptor != null && right.ExitDescriptor != null;
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        // May reach here without it computed if all elements are literals
        ComputeExitTypeDescriptor();

        cf.EnterCompilationScope();
        children[0].GenerateCode(gen, cf);
        TypeDescriptor lastDesc = cf.LastDescriptor() ?? throw new InvalidOperationException("No last descriptor");

        if (!CodeFlow.IsValueType(lastDesc))
        {
            gen.Emit(OpCodes.Unbox_Any, typeof(bool));
        }

        cf.ExitCompilationScope();

        Label elseTarget = gen.DefineLabel();
        Label endOfIfTarget = gen.DefineLabel();

        gen.Emit(OpCodes.Brfalse, elseTarget);
        cf.EnterCompilationScope();
        children[1].GenerateCode(gen, cf);

        if (!CodeFlow.IsValueType(exitTypeDescriptor))
        {
            lastDesc = cf.LastDescriptor();

            if (lastDesc == null)
            {
                throw new InvalidOperationException("No last descriptor");
            }

            CodeFlow.InsertBoxIfNecessary(gen, lastDesc);
        }

        cf.ExitCompilationScope();
        gen.Emit(OpCodes.Br, endOfIfTarget);

        gen.MarkLabel(elseTarget);
        cf.EnterCompilationScope();
        children[2].GenerateCode(gen, cf);

        if (!CodeFlow.IsValueType(exitTypeDescriptor))
        {
            lastDesc = cf.LastDescriptor();

            if (lastDesc == null)
            {
                throw new InvalidOperationException("No last descriptor");
            }

            CodeFlow.InsertBoxIfNecessary(gen, lastDesc);
        }

        cf.ExitCompilationScope();
        gen.MarkLabel(endOfIfTarget);
        cf.PushDescriptor(exitTypeDescriptor);
    }

    private void ComputeExitTypeDescriptor()
    {
        if (exitTypeDescriptor == null && children[1].ExitDescriptor != null && children[2].ExitDescriptor != null)
        {
            TypeDescriptor leftDescriptor = children[1].ExitDescriptor;
            TypeDescriptor rightDescriptor = children[2].ExitDescriptor;

            if (ObjectUtils.NullSafeEquals(leftDescriptor, rightDescriptor))
            {
                exitTypeDescriptor = leftDescriptor;
            }
            else
            {
                // Use the easiest to compute common super type
                exitTypeDescriptor = TypeDescriptor.Object;
            }
        }
    }
}
