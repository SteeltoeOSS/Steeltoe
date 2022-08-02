// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpModulus : Operator
{
    public OpModulus(int startPos, int endPos, params SpelNode[] operands)
        : base("%", startPos, endPos, operands)
    {
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        object leftOperand = LeftOperand.GetValueInternal(state).Value;
        object rightOperand = RightOperand.GetValueInternal(state).Value;

        if (IsNumber(leftOperand) && IsNumber(rightOperand))
        {
            var leftNumber = (IConvertible)leftOperand;
            var rightNumber = (IConvertible)rightOperand;

            if (leftNumber is decimal || rightNumber is decimal)
            {
                decimal leftVal = leftNumber.ToDecimal(CultureInfo.InvariantCulture);
                decimal rightVal = rightNumber.ToDecimal(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal % rightVal);
            }

            if (leftNumber is double || rightNumber is double)
            {
                exitTypeDescriptor = TypeDescriptor.D;
                double leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                double rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal % rightVal);
            }

            if (leftNumber is float || rightNumber is float)
            {
                exitTypeDescriptor = TypeDescriptor.F;
                float leftVal = leftNumber.ToSingle(CultureInfo.InvariantCulture);
                float rightVal = rightNumber.ToSingle(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal % rightVal);
            }

            if (leftNumber is long || rightNumber is long)
            {
                exitTypeDescriptor = TypeDescriptor.J;
                long leftVal = leftNumber.ToInt64(CultureInfo.InvariantCulture);
                long rightVal = rightNumber.ToInt64(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal % rightVal);
            }

            if (CodeFlow.IsIntegerForNumericOp(leftNumber) || CodeFlow.IsIntegerForNumericOp(rightNumber))
            {
                exitTypeDescriptor = TypeDescriptor.I;
                int leftVal = leftNumber.ToInt32(CultureInfo.InvariantCulture);
                int rightVal = rightNumber.ToInt32(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal % rightVal);
            }
            else
            {
                // Unknown Number subtypes -> best guess is double division
                double leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                double rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal % rightVal);
            }
        }

        return state.Operate(Operation.Modulus, leftOperand, rightOperand);
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
            gen.Emit(OpCodes.Rem);
        }

        cf.PushDescriptor(exitTypeDescriptor);
    }
}
