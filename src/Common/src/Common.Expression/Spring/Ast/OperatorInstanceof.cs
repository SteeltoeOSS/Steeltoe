// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OperatorInstanceof : Operator
{
    private Type _type;

    public OperatorInstanceof(int startPos, int endPos, params SpelNode[] operands)
        : base("instanceof", startPos, endPos, operands)
    {
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        var rightOperand = RightOperand;
        var left = LeftOperand.GetValueInternal(state);
        var right = rightOperand.GetValueInternal(state);
        var leftValue = left.Value;
        var rightValue = right.Value;
        BooleanTypedValue result;
        if (rightValue is not Type rightClass)
        {
            throw new SpelEvaluationException(RightOperand.StartPosition, SpelMessage.INSTANCEOF_OPERATOR_NEEDS_CLASS_OPERAND, rightValue == null ? "null" : rightValue.GetType().FullName);
        }

        if (leftValue == null)
        {
            result = BooleanTypedValue.FALSE;  // null is not an instance of anything
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
            _exitTypeDescriptor = TypeDescriptor.Z;
        }

        return result;
    }

    public override bool IsCompilable()
    {
        return _exitTypeDescriptor != null && LeftOperand.IsCompilable();
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        LeftOperand.GenerateCode(gen, cf);
        CodeFlow.InsertBoxIfNecessary(gen, cf.LastDescriptor());
        if (_type == null)
        {
            throw new InvalidOperationException("No type available");
        }

        var convert = gen.DeclareLocal(typeof(bool));
        gen.Emit(OpCodes.Isinst, _type);
        gen.Emit(OpCodes.Ldnull);
        gen.Emit(OpCodes.Cgt_Un);
        gen.Emit(OpCodes.Stloc, convert);
        gen.Emit(OpCodes.Ldloc, convert);

        cf.PushDescriptor(_exitTypeDescriptor);
    }
}
