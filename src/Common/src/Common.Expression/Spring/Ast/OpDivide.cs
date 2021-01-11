// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class OpDivide : Operator
    {
        public OpDivide(int startPos, int endPos, params SpelNode[] operands)
            : base("/", startPos, endPos, operands)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var leftOperand = LeftOperand.GetValueInternal(state).Value;
            var rightOperand = RightOperand.GetValueInternal(state).Value;

            if (IsNumber(leftOperand) && IsNumber(rightOperand))
            {
                var leftConv = (IConvertible)leftOperand;
                var rightConv = (IConvertible)rightOperand;

                if (leftOperand is decimal || rightOperand is decimal)
                {
                    var leftVal = leftConv.ToDecimal(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToDecimal(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal / rightVal);
                }
                else if (leftOperand is double || rightOperand is double)
                {
                    var leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToDouble(CultureInfo.InvariantCulture);
                    _exitTypeDescriptor = "D";
                    return new TypedValue(leftVal / rightVal);
                }
                else if (leftOperand is float || rightOperand is float)
                {
                    var leftVal = leftConv.ToSingle(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToSingle(CultureInfo.InvariantCulture);
                    _exitTypeDescriptor = "F";
                    return new TypedValue(leftVal / rightVal);
                }

                // else if (leftNumber instanceof BigInteger || rightNumber instanceof BigInteger) {
                //    BigInteger leftBigInteger = NumberUtils.convertNumberToTargetClass(leftNumber, BigInteger.class);
                // BigInteger rightBigInteger = NumberUtils.convertNumberToTargetClass(rightNumber, BigInteger.class);
                // return new TypedValue(leftBigInteger.divide(rightBigInteger));
                // }

                // TODO: Look at need to add support for .NET types not present in Java, e.g. ulong, ushort, byte, uint
                else if (leftOperand is long || rightOperand is long)
                {
                    var leftVal = leftConv.ToInt64(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToInt64(CultureInfo.InvariantCulture);
                    _exitTypeDescriptor = "J";
                    return new TypedValue(leftVal / rightVal);
                }
                else if (CodeFlow.IsIntegerForNumericOp(leftOperand) || CodeFlow.IsIntegerForNumericOp(rightOperand))
                {
                    var leftVal = leftConv.ToInt32(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToInt32(CultureInfo.InvariantCulture);
                    _exitTypeDescriptor = "I";
                    return new TypedValue(leftVal / rightVal);
                }
                else
                {
                    var leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToDouble(CultureInfo.InvariantCulture);

                    // Unknown Number subtypes -> best guess is double division
                    // TODO: Look at need to add support for .NET types not present in Java, e.g. ulong, ushort, byte, uint
                    return new TypedValue(leftVal / rightVal);
                }
            }

            return state.Operate(Operation.DIVIDE, leftOperand, rightOperand);
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

        // public void GenerateCode(MethodVisitor mv, CodeFlow cf)
        // {
        //    getLeftOperand().generateCode(mv, cf);
        //    String leftDesc = getLeftOperand().exitTypeDescriptor;
        //    String exitDesc = this.exitTypeDescriptor;
        //    Assert.state(exitDesc != null, "No exit type descriptor");
        //    char targetDesc = exitDesc.charAt(0);
        //    CodeFlow.insertNumericUnboxOrPrimitiveTypeCoercion(mv, leftDesc, targetDesc);
        //    if (this.children.length > 1)
        //    {
        //        cf.enterCompilationScope();
        //        getRightOperand().generateCode(mv, cf);
        //        String rightDesc = getRightOperand().exitTypeDescriptor;
        //        cf.exitCompilationScope();
        //        CodeFlow.insertNumericUnboxOrPrimitiveTypeCoercion(mv, rightDesc, targetDesc);
        //        switch (targetDesc)
        //        {
        //            case 'I':
        //                mv.visitInsn(IDIV);
        //                break;
        //            case 'J':
        //                mv.visitInsn(LDIV);
        //                break;
        //            case 'F':
        //                mv.visitInsn(FDIV);
        //                break;
        //            case 'D':
        //                mv.visitInsn(DDIV);
        //                break;
        //            default:
        //                throw new IllegalStateException(
        //                        "Unrecognized exit type descriptor: '" + this.exitTypeDescriptor + "'");
        //        }
        //    }
        //    cf.pushDescriptor(this.exitTypeDescriptor);
        // }
    }
}
