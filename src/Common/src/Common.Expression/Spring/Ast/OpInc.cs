// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpInc : Operator
{
    private readonly bool _postfix;  // false means prefix

    public OpInc(int startPos, int endPos, bool postfix, params SpelNode[] operands)
        : base("++", startPos, endPos, operands)
    {
        _postfix = postfix;
        if (operands == null || operands.Length == 0)
        {
            throw new InvalidOperationException("Operands must not be empty");
        }
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        var operand = LeftOperand;
        var valueRef = operand.GetValueRef(state);

        var typedValue = valueRef.GetValue();
        var value = typedValue.Value;
        var returnValue = typedValue;
        ITypedValue newValue = null;

        if (IsNumber(value))
        {
            newValue = (IConvertible)value switch
            {
                decimal val => new TypedValue(val + 1M, typedValue.TypeDescriptor),
                double val => new TypedValue(val + 1.0d, typedValue.TypeDescriptor),
                float val => new TypedValue(val + 1.0f, typedValue.TypeDescriptor),
                long val => new TypedValue(val + 1L, typedValue.TypeDescriptor),
                int val => new TypedValue(val + 1, typedValue.TypeDescriptor),
                short val => new TypedValue((short)(val + 1), typedValue.TypeDescriptor),
                byte val => new TypedValue((byte)(val + 1), typedValue.TypeDescriptor),
                ulong val => new TypedValue(val + 1UL, typedValue.TypeDescriptor),
                uint val => new TypedValue(val + 1U, typedValue.TypeDescriptor),
                ushort val => new TypedValue((ushort)(val + 1), typedValue.TypeDescriptor),
                sbyte val => new TypedValue((sbyte)(val + 1), typedValue.TypeDescriptor),
                _ => null
            };
        }

        if (newValue == null)
        {
            try
            {
                newValue = state.Operate(Operation.ADD, returnValue.Value, 1);
            }
            catch (SpelEvaluationException ex)
            {
                if (ex.MessageCode == SpelMessage.OPERATOR_NOT_SUPPORTED_BETWEEN_TYPES)
                {
                    // This means the operand is not incrementable
                    throw new SpelEvaluationException(operand.StartPosition, SpelMessage.OPERAND_NOT_INCREMENTABLE, operand.ToStringAST());
                }

                throw;
            }
        }

        // set the name value
        try
        {
            valueRef.SetValue(newValue.Value);
        }
        catch (SpelEvaluationException see)
        {
            // If unable to set the value the operand is not writable (e.g. 1++ )
            if (see.MessageCode == SpelMessage.SETVALUE_NOT_SUPPORTED)
            {
                throw new SpelEvaluationException(operand.StartPosition, SpelMessage.OPERAND_NOT_INCREMENTABLE);
            }
            else
            {
                throw;
            }
        }

        if (!_postfix)
        {
            // The return value is the new value, not the original value
            returnValue = newValue;
        }

        return returnValue;
    }

    public override string ToStringAST()
    {
        return LeftOperand.ToStringAST() + "++";
    }

    public override SpelNode RightOperand => throw new InvalidOperationException("No right operand");
}