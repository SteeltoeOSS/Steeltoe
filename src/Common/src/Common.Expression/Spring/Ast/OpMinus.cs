// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class OpMinus : Operator
    {
        public OpMinus(int startPos, int endPos, params SpelNode[] operands)
            : base("-", startPos, endPos, operands)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var leftOp = LeftOperand;

            if (_children.Length < 2)
            {
                // if only one operand, then this is unary minus
                var operand = leftOp.GetValueInternal(state).Value;
                if (IsNumber(operand))
                {
                    if (operand is decimal)
                    {
                        return new TypedValue(0M - ((decimal)operand));
                    }
                    else if (operand is double)
                    {
                        _exitTypeDescriptor = TypeDescriptor.D;
                        return new TypedValue(0d - ((double)operand));
                    }
                    else if (operand is float)
                    {
                        _exitTypeDescriptor = TypeDescriptor.F;
                        return new TypedValue(0f - ((float)operand));
                    }
                    else if (operand is long)
                    {
                        _exitTypeDescriptor = TypeDescriptor.J;
                        return new TypedValue(0L - ((long)operand));
                    }
                    else if (operand is int)
                    {
                        _exitTypeDescriptor = TypeDescriptor.I;
                        return new TypedValue(0 - ((int)operand));
                    }
                    else if (operand is short)
                    {
                        return new TypedValue(((short)0) - ((short)operand));
                    }
                    else if (operand is byte)
                    {
                        return new TypedValue(((byte)0) - ((byte)operand));
                    }
                    else if (operand is ulong)
                    {
                        return new TypedValue(0UL - ((ulong)operand));
                    }
                    else if (operand is uint)
                    {
                        return new TypedValue(0U - ((uint)operand));
                    }
                    else if (operand is ushort)
                    {
                        return new TypedValue(((ushort)0) - ((ushort)operand));
                    }
                    else if (operand is sbyte)
                    {
                        return new TypedValue(((sbyte)0) - ((sbyte)operand));
                    }
                }

                return state.Operate(Operation.SUBTRACT, operand, null);
            }

            var left = leftOp.GetValueInternal(state).Value;
            var right = RightOperand.GetValueInternal(state).Value;

            if (IsNumber(left) && IsNumber(right))
            {
                var leftNumber = (IConvertible)left;
                var rightNumber = (IConvertible)right;

                if (leftNumber is decimal || rightNumber is decimal)
                {
                    var leftVal = leftNumber.ToDecimal(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToDecimal(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal - rightVal);
                }
                else if (leftNumber is double || rightNumber is double)
                {
                    _exitTypeDescriptor = TypeDescriptor.D;
                    var leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal - rightVal);
                }
                else if (leftNumber is float || rightNumber is float)
                {
                    _exitTypeDescriptor = TypeDescriptor.F;
                    var leftVal = leftNumber.ToSingle(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToSingle(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal - rightVal);
                }
                else if (leftNumber is long || rightNumber is long)
                {
                    _exitTypeDescriptor = TypeDescriptor.J;
                    var leftVal = leftNumber.ToInt64(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToInt64(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal - rightVal);
                }
                else if (CodeFlow.IsIntegerForNumericOp(leftNumber) || CodeFlow.IsIntegerForNumericOp(rightNumber))
                {
                    _exitTypeDescriptor = TypeDescriptor.I;
                    var leftVal = leftNumber.ToInt32(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToInt32(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal - rightVal);
                }
                else
                {
                    // Unknown Number subtypes -> best guess is double subtraction
                    var leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal - rightVal);
                }
            }

            if (left is string && right is int && ((string)left).Length == 1)
            {
                var theString = (string)left;
                var theInteger = (int)right;

                // Implements character - int (ie. b - 1 = a)
                return new TypedValue(((char)(theString[0] - theInteger)).ToString());
            }

            return state.Operate(Operation.SUBTRACT, left, right);
        }

        public override string ToStringAST()
        {
            if (_children.Length < 2)
            {
                // unary minus
                return "-" + LeftOperand.ToStringAST();
            }

            return base.ToStringAST();
        }

        public override SpelNode RightOperand
        {
            get
            {
                if (_children.Length < 2)
                {
                    throw new InvalidOperationException("No right operand");
                }

                return _children[1];
            }
        }

        public override bool IsCompilable()
        {
            if (!LeftOperand.IsCompilable())
            {
                return false;
            }

            if (_children.Length > 1 && !RightOperand.IsCompilable())
            {
                return false;
            }

            return _exitTypeDescriptor != null;
        }

        public override void GenerateCode(ILGenerator gen, CodeFlow cf)
        {
            LeftOperand.GenerateCode(gen, cf);
            var leftDesc = LeftOperand.ExitDescriptor;
            var exitDesc = _exitTypeDescriptor;
            if (exitDesc == null)
            {
                throw new InvalidOperationException("No exit type descriptor");
            }

            CodeFlow.InsertNumericUnboxOrPrimitiveTypeCoercion(gen, leftDesc, exitDesc);
            if (_children.Length > 1)
            {
                cf.EnterCompilationScope();
                RightOperand.GenerateCode(gen, cf);
                var rightDesc = RightOperand.ExitDescriptor;
                cf.ExitCompilationScope();
                CodeFlow.InsertNumericUnboxOrPrimitiveTypeCoercion(gen, rightDesc, exitDesc);
                gen.Emit(OpCodes.Sub);
            }
            else
            {
                gen.Emit(OpCodes.Neg);
            }

            cf.PushDescriptor(_exitTypeDescriptor);
        }
    }
}
