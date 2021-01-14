// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class OpPlus : Operator
    {
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
                        _exitTypeDescriptor = "D";
                    }
                    else if (operandOne is float)
                    {
                        _exitTypeDescriptor = "F";
                    }
                    else if (operandOne is long)
                    {
                        _exitTypeDescriptor = "J";
                    }
                    else if (operandOne is int)
                    {
                        _exitTypeDescriptor = "I";
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
                    _exitTypeDescriptor = "D";
                    var leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal + rightVal);
                }
                else if (leftNumber is float || rightNumber is float)
                {
                    _exitTypeDescriptor = "F";
                    var leftVal = leftNumber.ToSingle(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToSingle(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal + rightVal);
                }
                else if (leftNumber is long || rightNumber is long)
                {
                    _exitTypeDescriptor = "J";
                    var leftVal = leftNumber.ToInt64(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToInt64(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal + rightVal);
                }
                else if (CodeFlow.IsIntegerForNumericOp(leftNumber) || CodeFlow.IsIntegerForNumericOp(rightNumber))
                {
                    _exitTypeDescriptor = "I";
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

            if (leftOperand is string && rightOperand is string)
            {
                _exitTypeDescriptor = "LSystem/String";
                return new TypedValue((string)leftOperand + (string)rightOperand);
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

        // private void Walk(DynamicMethod mv, CodeFlow cf, SpelNode operand)
        // {
        //    if (operand is OpPlus) {
        //        OpPlus plus = (OpPlus)operand;
        //        walk(mv, cf, plus.getLeftOperand());
        //        walk(mv, cf, plus.getRightOperand());
        //    }

        // else if (operand != null)
        //    {
        //        cf.enterCompilationScope();
        //        operand.generateCode(mv, cf);
        //        if (!"Ljava/lang/String".equals(cf.lastDescriptor()))
        //        {
        //            mv.visitTypeInsn(CHECKCAST, "java/lang/String");
        //        }
        //        cf.exitCompilationScope();
        //        mv.visitMethodInsn(INVOKEVIRTUAL, "java/lang/StringBuilder", "append", "(Ljava/lang/String;)Ljava/lang/StringBuilder;", false);
        //    }
        // }
        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // if ("Ljava/lang/String".equals(this.exitTypeDescriptor))
            //    {
            //        mv.visitTypeInsn(NEW, "java/lang/StringBuilder");
            //        mv.visitInsn(DUP);
            //        mv.visitMethodInsn(INVOKESPECIAL, "java/lang/StringBuilder", "<init>", "()V", false);
            //        walk(mv, cf, getLeftOperand());
            //        walk(mv, cf, getRightOperand());
            //        mv.visitMethodInsn(INVOKEVIRTUAL, "java/lang/StringBuilder", "toString", "()Ljava/lang/String;", false);
            //    }
            //    else
            //    {
            //        this.children[0].generateCode(mv, cf);
            //        String leftDesc = this.children[0].exitTypeDescriptor;
            //        String exitDesc = this.exitTypeDescriptor;
            //        Assert.state(exitDesc != null, "No exit type descriptor");
            //        char targetDesc = exitDesc.charAt(0);
            //        CodeFlow.insertNumericUnboxOrPrimitiveTypeCoercion(mv, leftDesc, targetDesc);
            //        if (this.children.length > 1)
            //        {
            //            cf.enterCompilationScope();
            //            this.children[1].generateCode(mv, cf);
            //            String rightDesc = this.children[1].exitTypeDescriptor;
            //            cf.exitCompilationScope();
            //            CodeFlow.insertNumericUnboxOrPrimitiveTypeCoercion(mv, rightDesc, targetDesc);
            //            switch (targetDesc)
            //            {
            //                case 'I':
            //                    mv.visitInsn(IADD);
            //                    break;
            //                case 'J':
            //                    mv.visitInsn(LADD);
            //                    break;
            //                case 'F':
            //                    mv.visitInsn(FADD);
            //                    break;
            //                case 'D':
            //                    mv.visitInsn(DADD);
            //                    break;
            //                default:
            //                    throw new IllegalStateException(
            //                            "Unrecognized exit type descriptor: '" + this.exitTypeDescriptor + "'");
            //            }
            //        }
            //    }
            //    cf.pushDescriptor(this.exitTypeDescriptor);
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
    }
}
