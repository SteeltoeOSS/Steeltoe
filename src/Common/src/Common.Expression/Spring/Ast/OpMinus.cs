// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpMinus : Operator
{
    public override SpelNode RightOperand
    {
        get
        {
            if (children.Length < 2)
            {
                throw new InvalidOperationException("No right operand");
            }

            return children[1];
        }
    }

    public OpMinus(int startPos, int endPos, params SpelNode[] operands)
        : base("-", startPos, endPos, operands)
    {
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        SpelNode leftOp = LeftOperand;

        if (children.Length < 2)
        {
            // if only one operand, then this is unary minus
            object operand = leftOp.GetValueInternal(state).Value;

            if (IsNumber(operand))
            {
                switch (operand)
                {
                    case decimal val:
                        return new TypedValue(0M - val);
                    case double val:
                        exitTypeDescriptor = TypeDescriptor.D;
                        return new TypedValue(0d - val);
                    case float val:
                        exitTypeDescriptor = TypeDescriptor.F;
                        return new TypedValue(0f - val);
                    case long val:
                        exitTypeDescriptor = TypeDescriptor.J;
                        return new TypedValue(0L - val);
                    case int val:
                        exitTypeDescriptor = TypeDescriptor.I;
                        return new TypedValue(0 - val);
                    case short val:
                        return new TypedValue(0 - val);
                    case byte val:
                        return new TypedValue(0 - val);
                    case ulong val:
                        return new TypedValue(0UL - val);
                    case uint val:
                        return new TypedValue(0U - val);
                    case ushort val:
                        return new TypedValue(0 - val);
                    case sbyte val:
                        return new TypedValue(0 - val);
                    default:
                        return state.Operate(Operation.Subtract, operand, null);
                }
            }

            return state.Operate(Operation.Subtract, operand, null);
        }

        object left = leftOp.GetValueInternal(state).Value;
        object right = RightOperand.GetValueInternal(state).Value;

        if (IsNumber(left) && IsNumber(right))
        {
            var leftNumber = (IConvertible)left;
            var rightNumber = (IConvertible)right;

            if (leftNumber is decimal || rightNumber is decimal)
            {
                decimal leftVal = leftNumber.ToDecimal(CultureInfo.InvariantCulture);
                decimal rightVal = rightNumber.ToDecimal(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal - rightVal);
            }

            if (leftNumber is double || rightNumber is double)
            {
                exitTypeDescriptor = TypeDescriptor.D;
                double leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                double rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal - rightVal);
            }

            if (leftNumber is float || rightNumber is float)
            {
                exitTypeDescriptor = TypeDescriptor.F;
                float leftVal = leftNumber.ToSingle(CultureInfo.InvariantCulture);
                float rightVal = rightNumber.ToSingle(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal - rightVal);
            }

            if (leftNumber is long || rightNumber is long)
            {
                exitTypeDescriptor = TypeDescriptor.J;
                long leftVal = leftNumber.ToInt64(CultureInfo.InvariantCulture);
                long rightVal = rightNumber.ToInt64(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal - rightVal);
            }

            if (CodeFlow.IsIntegerForNumericOp(leftNumber) || CodeFlow.IsIntegerForNumericOp(rightNumber))
            {
                exitTypeDescriptor = TypeDescriptor.I;
                int leftVal = leftNumber.ToInt32(CultureInfo.InvariantCulture);
                int rightVal = rightNumber.ToInt32(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal - rightVal);
            }
            else
            {
                // Unknown Number subtypes -> best guess is double subtraction
                double leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                double rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal - rightVal);
            }
        }

        if (left is string str && right is int integer && str.Length == 1)
        {
            string theString = str;
            int theInteger = integer;

            // Implements character - int (ie. b - 1 = a)
            return new TypedValue(((char)(theString[0] - theInteger)).ToString());
        }

        return state.Operate(Operation.Subtract, left, right);
    }

    public override string ToStringAst()
    {
        if (children.Length < 2)
        {
            // unary minus
            return $"-{LeftOperand.ToStringAst()}";
        }

        return base.ToStringAst();
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
            gen.Emit(OpCodes.Sub);
        }
        else
        {
            gen.Emit(OpCodes.Neg);
        }

        cf.PushDescriptor(exitTypeDescriptor);
    }
}
