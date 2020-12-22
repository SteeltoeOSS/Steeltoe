// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class OpModulus : Operator
    {
        public OpModulus(int startPos, int endPos, params SpelNode[] operands)
            : base("%", startPos, endPos, operands)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var leftOperand = LeftOperand.GetValueInternal(state).Value;
            var rightOperand = RightOperand.GetValueInternal(state).Value;

            if (IsNumber(leftOperand) && IsNumber(rightOperand))
            {
                var leftNumber = (IConvertible)leftOperand;
                var rightNumber = (IConvertible)rightOperand;

                if (leftNumber is decimal || rightNumber is decimal)
                {
                    var leftVal = leftNumber.ToDecimal(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToDecimal(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal % rightVal);
                }
                else if (leftNumber is double || rightNumber is double)
                {
                    _exitTypeDescriptor = "D";
                    var leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal % rightVal);
                }
                else if (leftNumber is float || rightNumber is float)
                {
                    _exitTypeDescriptor = "F";
                    var leftVal = leftNumber.ToSingle(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToSingle(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal % rightVal);
                }
                else if (leftNumber is long || rightNumber is long)
                {
                    _exitTypeDescriptor = "J";
                    var leftVal = leftNumber.ToInt64(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToInt64(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal % rightVal);
                }
                else if (CodeFlow.IsIntegerForNumericOp(leftNumber) || CodeFlow.IsIntegerForNumericOp(rightNumber))
                {
                    _exitTypeDescriptor = "I";
                    var leftVal = leftNumber.ToInt32(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToInt32(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal % rightVal);
                }
                else
                {
                    // Unknown Number subtypes -> best guess is double division
                    var leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                    var rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                    return new TypedValue(leftVal % rightVal);
                }
            }

            return state.Operate(Operation.MODULUS, leftOperand, rightOperand);
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

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // getLeftOperand().generateCode(mv, cf);
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
            //                mv.visitInsn(IREM);
            //                break;
            //            case 'J':
            //                mv.visitInsn(LREM);
            //                break;
            //            case 'F':
            //                mv.visitInsn(FREM);
            //                break;
            //            case 'D':
            //                mv.visitInsn(DREM);
            //                break;
            //            default:
            //                throw new IllegalStateException(
            //                        "Unrecognized exit type descriptor: '" + this.exitTypeDescriptor + "'");
            //        }
            //    }
            //    cf.pushDescriptor(this.exitTypeDescriptor);
        }
    }
}
