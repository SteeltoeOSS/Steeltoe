// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
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
                var op1 = (IConvertible)value;
                if (op1 is decimal val1)
                {
                    newValue = new TypedValue(val1 + 1M, typedValue.TypeDescriptor);
                }
                else if (op1 is double val2)
                {
                    newValue = new TypedValue(val2 + 1.0d, typedValue.TypeDescriptor);
                }
                else if (op1 is float val3)
                {
                    newValue = new TypedValue(val3 + 1.0f, typedValue.TypeDescriptor);
                }
                else if (op1 is long val4)
                {
                    newValue = new TypedValue(val4 + 1L, typedValue.TypeDescriptor);
                }
                else if (op1 is int val5)
                {
                    newValue = new TypedValue(val5 + 1, typedValue.TypeDescriptor);
                }
                else if (op1 is short val6)
                {
                    newValue = new TypedValue((short)(val6 + (short)1), typedValue.TypeDescriptor);
                }
                else if (op1 is byte val7)
                {
                    newValue = new TypedValue((byte)(val7 + (byte)1), typedValue.TypeDescriptor);
                }
                else if (op1 is ulong val8)
                {
                    newValue = new TypedValue((ulong)(val8 + 1UL), typedValue.TypeDescriptor);
                }
                else if (op1 is uint val9)
                {
                    newValue = new TypedValue((uint)(val9 + 1U), typedValue.TypeDescriptor);
                }
                else if (op1 is ushort val10)
                {
                    newValue = new TypedValue((ushort)(val10 + (ushort)1), typedValue.TypeDescriptor);
                }
                else if (op1 is sbyte val11)
                {
                    newValue = new TypedValue((sbyte)(val11 + (sbyte)1), typedValue.TypeDescriptor);
                }
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
}
