// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection.Emit;
using Steeltoe.Common.Expression.Internal.Spring.Support;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OperatorInstanceOf : Operator
{
    private Type _type;

    public OperatorInstanceOf(int startPos, int endPos, params SpelNode[] operands)
        : base("instanceof", startPos, endPos, operands)
    {
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        SpelNode rightOperand = RightOperand;
        ITypedValue left = LeftOperand.GetValueInternal(state);
        ITypedValue right = rightOperand.GetValueInternal(state);
        object leftValue = left.Value;
        object rightValue = right.Value;
        BooleanTypedValue result;

        if (rightValue is not Type rightClass)
        {
            throw new SpelEvaluationException(RightOperand.StartPosition, SpelMessage.InstanceOfOperatorNeedsClassOperand,
                rightValue == null ? "null" : rightValue.GetType().FullName);
        }

        if (leftValue == null)
        {
            result = BooleanTypedValue.False; // null is not an instance of anything
        }
        else
        {
            result = BooleanTypedValue.ForValue(rightClass.IsInstanceOfType(leftValue));
        }

        _type = rightClass;

        if (rightOperand is TypeReference)
        {
            // Can only generate bytecode where the right operand is a direct type reference,
            // not if it is indirect (for example when right operand is a variable reference)
            exitTypeDescriptor = TypeDescriptor.Z;
        }

        return result;
    }

    public override bool IsCompilable()
    {
        return exitTypeDescriptor != null && LeftOperand.IsCompilable();
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        LeftOperand.GenerateCode(gen, cf);
        CodeFlow.InsertBoxIfNecessary(gen, cf.LastDescriptor());

        if (_type == null)
        {
            throw new InvalidOperationException("No type available");
        }

        LocalBuilder convert = gen.DeclareLocal(typeof(bool));
        gen.Emit(OpCodes.Isinst, _type);
        gen.Emit(OpCodes.Ldnull);
        gen.Emit(OpCodes.Cgt_Un);
        gen.Emit(OpCodes.Stloc, convert);
        gen.Emit(OpCodes.Ldloc, convert);

        cf.PushDescriptor(exitTypeDescriptor);
    }
}
