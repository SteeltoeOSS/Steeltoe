// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpDivide : Operator
{
    public OpDivide(int startPos, int endPos, params SpelNode[] operands)
        : base("/", startPos, endPos, operands)
    {
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        object leftOperand = LeftOperand.GetValueInternal(state).Value;
        object rightOperand = RightOperand.GetValueInternal(state).Value;

        if (IsNumber(leftOperand) && IsNumber(rightOperand))
        {
            var leftConv = (IConvertible)leftOperand;
            var rightConv = (IConvertible)rightOperand;

            if (leftOperand is decimal || rightOperand is decimal)
            {
                decimal leftVal = leftConv.ToDecimal(CultureInfo.InvariantCulture);
                decimal rightVal = rightConv.ToDecimal(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal / rightVal);
            }

            if (leftOperand is double || rightOperand is double)
            {
                double leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                double rightVal = rightConv.ToDouble(CultureInfo.InvariantCulture);
                exitTypeDescriptor = TypeDescriptor.D;
                return new TypedValue(leftVal / rightVal);
            }

            if (leftOperand is float || rightOperand is float)
            {
                float leftVal = leftConv.ToSingle(CultureInfo.InvariantCulture);
                float rightVal = rightConv.ToSingle(CultureInfo.InvariantCulture);
                exitTypeDescriptor = TypeDescriptor.F;
                return new TypedValue(leftVal / rightVal);
            }

            // Look at need to add support for .NET types not present in Java, e.g. ulong, ushort, byte, uint

            if (leftOperand is long || rightOperand is long)
            {
                long leftVal = leftConv.ToInt64(CultureInfo.InvariantCulture);
                long rightVal = rightConv.ToInt64(CultureInfo.InvariantCulture);
                exitTypeDescriptor = TypeDescriptor.J;
                return new TypedValue(leftVal / rightVal);
            }

            if (CodeFlow.IsIntegerForNumericOp(leftOperand) || CodeFlow.IsIntegerForNumericOp(rightOperand))
            {
                int leftVal = leftConv.ToInt32(CultureInfo.InvariantCulture);
                int rightVal = rightConv.ToInt32(CultureInfo.InvariantCulture);
                exitTypeDescriptor = TypeDescriptor.I;
                return new TypedValue(leftVal / rightVal);
            }
            else
            {
                double leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                double rightVal = rightConv.ToDouble(CultureInfo.InvariantCulture);

                // Unknown Number subtypes -> best guess is double division
                // Look at need to add support for .NET types not present in Java, e.g. ulong, ushort, byte, uint
                return new TypedValue(leftVal / rightVal);
            }
        }

        return state.Operate(Operation.Divide, leftOperand, rightOperand);
    }

    public override bool IsCompilable()
    {
        if (!LeftOperand.IsCompilable())
        {
            return false;
        }

        if (children.Length > 1 && !RightOperand.IsCompilable())
        {
            return false;
        }

        return exitTypeDescriptor != null;
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        LeftOperand.GenerateCode(gen, cf);
        TypeDescriptor leftDesc = LeftOperand.ExitDescriptor;
        TypeDescriptor exitDesc = exitTypeDescriptor;

        if (exitDesc == null)
        {
            throw new InvalidOperationException("No exit type descriptor");
        }

        CodeFlow.InsertNumericUnboxOrPrimitiveTypeCoercion(gen, leftDesc, exitDesc);

        if (children.Length > 1)
        {
            cf.EnterCompilationScope();
            RightOperand.GenerateCode(gen, cf);
            TypeDescriptor rightDesc = RightOperand.ExitDescriptor;
            cf.ExitCompilationScope();
            CodeFlow.InsertNumericUnboxOrPrimitiveTypeCoercion(gen, rightDesc, exitDesc);

            if (exitDesc != TypeDescriptor.I && exitDesc != TypeDescriptor.J && exitDesc != TypeDescriptor.F && exitDesc != TypeDescriptor.D)
            {
                throw new InvalidOperationException($"Unrecognized exit type descriptor: '{exitTypeDescriptor}'");
            }

            gen.Emit(OpCodes.Div);
        }

        cf.PushDescriptor(exitTypeDescriptor);
    }
}
