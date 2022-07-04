// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpMultiply : Operator
{
    public OpMultiply(int startPos, int endPos, params SpelNode[] operands)
        : base("*", startPos, endPos, operands)
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
                return new TypedValue(leftVal * rightVal);
            }
            else if (leftNumber is double || rightNumber is double)
            {
                exitTypeDescriptor = TypeDescriptor.D;
                var leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                var rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal * rightVal);
            }
            else if (leftNumber is float || rightNumber is float)
            {
                exitTypeDescriptor = TypeDescriptor.F;
                var leftVal = leftNumber.ToSingle(CultureInfo.InvariantCulture);
                var rightVal = rightNumber.ToSingle(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal * rightVal);
            }
            else if (leftNumber is long || rightNumber is long)
            {
                exitTypeDescriptor = TypeDescriptor.J;
                var leftVal = leftNumber.ToInt64(CultureInfo.InvariantCulture);
                var rightVal = rightNumber.ToInt64(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal * rightVal);
            }
            else if (CodeFlow.IsIntegerForNumericOp(leftNumber) || CodeFlow.IsIntegerForNumericOp(rightNumber))
            {
                exitTypeDescriptor = TypeDescriptor.I;
                var leftVal = leftNumber.ToInt32(CultureInfo.InvariantCulture);
                var rightVal = rightNumber.ToInt32(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal * rightVal);
            }
            else
            {
                // Unknown Number subtypes -> best guess is double multiplication
                var leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                var rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal * rightVal);
            }
        }

        if (leftOperand is string && rightOperand is int integer)
        {
            var repeats = integer;
            var result = new StringBuilder();
            for (var i = 0; i < repeats; i++)
            {
                result.Append(leftOperand);
            }

            return new TypedValue(result.ToString());
        }

        return state.Operate(Operation.Multiply, leftOperand, rightOperand);
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
        var leftDesc = LeftOperand.ExitDescriptor;
        var exitDesc = exitTypeDescriptor;
        if (exitDesc == null)
        {
            throw new InvalidOperationException("No exit type descriptor");
        }

        CodeFlow.InsertNumericUnboxOrPrimitiveTypeCoercion(gen, leftDesc, exitDesc);
        if (children.Length > 1)
        {
            cf.EnterCompilationScope();
            RightOperand.GenerateCode(gen, cf);
            var rightDesc = RightOperand.ExitDescriptor;
            cf.ExitCompilationScope();
            CodeFlow.InsertNumericUnboxOrPrimitiveTypeCoercion(gen, rightDesc, exitDesc);
            gen.Emit(OpCodes.Mul);
        }

        cf.PushDescriptor(exitTypeDescriptor);
    }
}
