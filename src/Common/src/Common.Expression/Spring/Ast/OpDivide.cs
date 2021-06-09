// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Reflection.Emit;

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
                    _exitTypeDescriptor = TypeDescriptor.D;
                    return new TypedValue(leftVal / rightVal);
                }
                else if (leftOperand is float || rightOperand is float)
                {
                    var leftVal = leftConv.ToSingle(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToSingle(CultureInfo.InvariantCulture);
                    _exitTypeDescriptor = TypeDescriptor.F;
                    return new TypedValue(leftVal / rightVal);
                }

                // TODO: Look at need to add support for .NET types not present in Java, e.g. ulong, ushort, byte, uint
                else if (leftOperand is long || rightOperand is long)
                {
                    var leftVal = leftConv.ToInt64(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToInt64(CultureInfo.InvariantCulture);
                    _exitTypeDescriptor = TypeDescriptor.J;
                    return new TypedValue(leftVal / rightVal);
                }
                else if (CodeFlow.IsIntegerForNumericOp(leftOperand) || CodeFlow.IsIntegerForNumericOp(rightOperand))
                {
                    var leftVal = leftConv.ToInt32(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToInt32(CultureInfo.InvariantCulture);
                    _exitTypeDescriptor = TypeDescriptor.I;
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
                if (exitDesc != TypeDescriptor.I && exitDesc != TypeDescriptor.J && exitDesc != TypeDescriptor.F && exitDesc != TypeDescriptor.D)
                {
                    throw new InvalidOperationException("Unrecognized exit type descriptor: '" + _exitTypeDescriptor + "'");
                }

                gen.Emit(OpCodes.Div);
            }

            cf.PushDescriptor(_exitTypeDescriptor);
        }
    }
}
