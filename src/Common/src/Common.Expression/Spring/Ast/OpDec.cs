// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class OpDec : Operator
    {
        private readonly bool _postfix;  // false means prefix

        public OpDec(int startPos, int endPos, bool postfix, params SpelNode[] operands)
            : base("--", startPos, endPos, operands)
        {
            _postfix = postfix;
            if (operands == null || operands.Length == 0)
            {
                throw new InvalidOperationException("Operands can not be empty");
            }
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var operand = LeftOperand;

            // The operand is going to be read and then assigned to, we don't want to evaluate it twice.
            var lvalue = operand.GetValueRef(state);

            var operandTypedValue = lvalue.GetValue();
            var operandValue = operandTypedValue.Value;
            var returnValue = operandTypedValue;
            ITypedValue newValue = null;

            if (IsNumber(operandValue))
            {
                if (operandValue is decimal val)
                {
                    newValue = new TypedValue(val - 1M, operandTypedValue.TypeDescriptor);
                }
                else if (operandValue is double val1)
                {
                    newValue = new TypedValue(val1 - 1.0d, operandTypedValue.TypeDescriptor);
                }
                else if (operandValue is float val2)
                {
                    newValue = new TypedValue(val2 - 1.0f, operandTypedValue.TypeDescriptor);
                }
                else if (operandValue is long val3)
                {
                    newValue = new TypedValue(val3 - 1L, operandTypedValue.TypeDescriptor);
                }
                else if (operandValue is int val4)
                {
                    newValue = new TypedValue(val4 - 1, operandTypedValue.TypeDescriptor);
                }
                else if (operandValue is short val5)
                {
                    newValue = new TypedValue((short)(val5 - (short)1), operandTypedValue.TypeDescriptor);
                }
                else if (operandValue is byte val6)
                {
                    newValue = new TypedValue((byte)(val6 - (byte)1), operandTypedValue.TypeDescriptor);
                }
                else if (operandValue is ulong val7)
                {
                    newValue = new TypedValue((ulong)(val7 - 1L), operandTypedValue.TypeDescriptor);
                }
                else if (operandValue is uint val8)
                {
                    newValue = new TypedValue((uint)(val8 - 1), operandTypedValue.TypeDescriptor);
                }
                else if (operandValue is ushort val9)
                {
                    newValue = new TypedValue((ushort)(val9 - (ushort)1), operandTypedValue.TypeDescriptor);
                }
                else if (operandValue is sbyte val10)
                {
                    newValue = new TypedValue((sbyte)(val10 - (sbyte)1), operandTypedValue.TypeDescriptor);
                }
            }

            if (newValue == null)
            {
                try
                {
                    newValue = state.Operate(Operation.SUBTRACT, returnValue.Value, 1);
                }
                catch (SpelEvaluationException ex)
                {
                    if (ex.MessageCode == SpelMessage.OPERATOR_NOT_SUPPORTED_BETWEEN_TYPES)
                    {
                        // This means the operand is not decrementable
                        throw new SpelEvaluationException(operand.StartPosition, SpelMessage.OPERAND_NOT_DECREMENTABLE, operand.ToStringAST());
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // set the new value
            try
            {
                lvalue.SetValue(newValue.Value);
            }
            catch (SpelEvaluationException see)
            {
                // if unable to set the value the operand is not writable (e.g. 1-- )
                if (see.MessageCode == SpelMessage.SETVALUE_NOT_SUPPORTED)
                {
                    throw new SpelEvaluationException(operand.StartPosition, SpelMessage.OPERAND_NOT_DECREMENTABLE);
                }
                else
                {
                    throw;
                }
            }

            if (!_postfix)
            {
                // the return value is the new value, not the original value
                returnValue = newValue;
            }

            return returnValue;
        }

        public override string ToStringAST()
        {
            return LeftOperand.ToStringAST() + "--";
        }

        public override SpelNode RightOperand => throw new InvalidOperationException("No right operand");
    }
}
