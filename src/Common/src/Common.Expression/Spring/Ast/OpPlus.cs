// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class OpPlus : Operator
    {
        private static readonly MethodInfo _appendString = typeof(StringBuilder).GetMethod("Append", new Type[] { typeof(string) });
        private static readonly MethodInfo _toString = typeof(StringBuilder).GetMethod("ToString", Type.EmptyTypes);
        private static readonly ConstructorInfo _sbConstructor = typeof(StringBuilder).GetConstructor(Type.EmptyTypes);

        public OpPlus(int startPos, int endPos, params SpelNode[] operands)
        : base("+", startPos, endPos, operands)
        {
            if (operands == null || operands.Length == 0)
            {
                throw new ArgumentException("Operands must not be empty");
            }
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var leftOp = LeftOperand;

            if (_children.Length < 2)
            {
                // if only one operand, then this is unary plus
                var operandOne = leftOp.GetValueInternal(state).Value;
                if (IsNumber(operandOne))
                {
                    if (operandOne is double)
                    {
                        _exitTypeDescriptor = TypeDescriptor.D;
                    }
                    else if (operandOne is float)
                    {
                        _exitTypeDescriptor = TypeDescriptor.F;
                    }
                    else if (operandOne is long)
                    {
                        _exitTypeDescriptor = TypeDescriptor.J;
                    }
                    else if (operandOne is int)
                    {
                        _exitTypeDescriptor = TypeDescriptor.I;
                    }

                    return new TypedValue(operandOne);
                }

                return state.Operate(Operation.ADD, operandOne, null);
            }

            var operandOneValue = leftOp.GetValueInternal(state);
            var leftOperand = operandOneValue.Value;
            var operandTwoValue = RightOperand.GetValueInternal(state);
            var rightOperand = operandTwoValue.Value;

            if (IsNumber(leftOperand) && IsNumber(rightOperand))
            {
                var leftNumber = (IConvertible)leftOperand;
                var rightNumber = (IConvertible)rightOperand;

                if (leftNumber is decimal || rightNumber is decimal)
                {
                    var leftVal = leftNumber.ToDecimal(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToDecimal(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal + rightVal);
                }
                else if (leftNumber is double || rightNumber is double)
                {
                    _exitTypeDescriptor = TypeDescriptor.D;
                    var leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal + rightVal);
                }
                else if (leftNumber is float || rightNumber is float)
                {
                    _exitTypeDescriptor = TypeDescriptor.F;
                    var leftVal = leftNumber.ToSingle(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToSingle(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal + rightVal);
                }
                else if (leftNumber is long || rightNumber is long)
                {
                    _exitTypeDescriptor = TypeDescriptor.J;
                    var leftVal = leftNumber.ToInt64(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToInt64(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal + rightVal);
                }
                else if (CodeFlow.IsIntegerForNumericOp(leftNumber) || CodeFlow.IsIntegerForNumericOp(rightNumber))
                {
                    _exitTypeDescriptor = TypeDescriptor.I;
                    var leftVal = leftNumber.ToInt32(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToInt32(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal + rightVal);
                }
                else
                {
                    // Unknown Number subtypes -> best guess is double addition
                    var leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal + rightVal);
                }
            }

            if (leftOperand is string strLeft && rightOperand is string strRight)
            {
                _exitTypeDescriptor = TypeDescriptor.STRING;
                return new TypedValue(strLeft + strRight);
            }

            if (leftOperand is string)
            {
                return new TypedValue(leftOperand + (rightOperand == null ? "null" : ConvertTypedValueToString(operandTwoValue, state)));
            }

            if (rightOperand is string)
            {
                return new TypedValue((leftOperand == null ? "null" : ConvertTypedValueToString(operandOneValue, state)) + rightOperand);
            }

            return state.Operate(Operation.ADD, leftOperand, rightOperand);
        }

        public override string ToStringAST()
        {
            if (_children.Length < 2)
            {
                // unary plus
                return "+" + LeftOperand.ToStringAST();
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
            if (_exitTypeDescriptor == TypeDescriptor.STRING)
            {
                gen.Emit(OpCodes.Newobj, _sbConstructor);
                Walk(gen, cf, LeftOperand);
                Walk(gen, cf, RightOperand);
                gen.Emit(OpCodes.Callvirt, _toString);
            }
            else
            {
                _children[0].GenerateCode(gen, cf);
                var leftDesc = _children[0].ExitDescriptor;
                var exitDesc = _exitTypeDescriptor;
                if (exitDesc == null)
                {
                    throw new InvalidOperationException("No exit type descriptor");
                }

                CodeFlow.InsertNumericUnboxOrPrimitiveTypeCoercion(gen, leftDesc, exitDesc);
                if (_children.Length > 1)
                {
                    cf.EnterCompilationScope();
                    _children[1].GenerateCode(gen, cf);
                    var rightDesc = _children[1].ExitDescriptor;
                    cf.ExitCompilationScope();
                    CodeFlow.InsertNumericUnboxOrPrimitiveTypeCoercion(gen, rightDesc, exitDesc);
                    gen.Emit(OpCodes.Add);
                }
            }

            cf.PushDescriptor(_exitTypeDescriptor);
        }

        private static string ConvertTypedValueToString(ITypedValue value, ExpressionState state)
        {
            var typeConverter = state.EvaluationContext.TypeConverter;
            var typeDescriptor = typeof(string);
            if (typeConverter.CanConvert(value.TypeDescriptor, typeDescriptor))
            {
                var val = typeConverter.ConvertValue(value.Value, value.TypeDescriptor, typeDescriptor);
                return val == null ? "null" : val.ToString();
            }

            return value.Value == null ? "null" : value.Value.ToString();
        }

        private void Walk(ILGenerator gen, CodeFlow cf, SpelNode operand)
        {
            if (operand is OpPlus plus)
            {
                Walk(gen, cf, plus.LeftOperand);
                Walk(gen, cf, plus.RightOperand);
            }
            else if (operand != null)
            {
                cf.EnterCompilationScope();
                operand.GenerateCode(gen, cf);
                if (cf.LastDescriptor() != TypeDescriptor.STRING)
                {
                    gen.Emit(OpCodes.Castclass, typeof(string));
                }

                cf.ExitCompilationScope();
                gen.Emit(OpCodes.Callvirt, _appendString);
            }
        }
    }
}
