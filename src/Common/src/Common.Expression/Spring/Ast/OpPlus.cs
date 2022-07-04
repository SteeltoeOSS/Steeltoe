// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class OpPlus : Operator
{
    private static readonly MethodInfo AppendStringMethod = typeof(StringBuilder).GetMethod(nameof(StringBuilder.Append), new[] { typeof(string) });
    private static readonly MethodInfo ToStringMethod = typeof(StringBuilder).GetMethod(nameof(StringBuilder.ToString), Type.EmptyTypes);
    private static readonly ConstructorInfo StringBuilderConstructor = typeof(StringBuilder).GetConstructor(Type.EmptyTypes);

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

        if (children.Length < 2)
        {
            // if only one operand, then this is unary plus
            var operandOne = leftOp.GetValueInternal(state).Value;
            if (IsNumber(operandOne))
            {
                if (operandOne is double)
                {
                    exitTypeDescriptor = TypeDescriptor.D;
                }
                else if (operandOne is float)
                {
                    exitTypeDescriptor = TypeDescriptor.F;
                }
                else if (operandOne is long)
                {
                    exitTypeDescriptor = TypeDescriptor.J;
                }
                else if (operandOne is int)
                {
                    exitTypeDescriptor = TypeDescriptor.I;
                }

                return new TypedValue(operandOne);
            }

            return state.Operate(Operation.Add, operandOne, null);
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
                exitTypeDescriptor = TypeDescriptor.D;
                var leftVal = leftNumber.ToDouble(CultureInfo.InvariantCulture);
                var rightVal = rightNumber.ToDouble(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal + rightVal);
            }
            else if (leftNumber is float || rightNumber is float)
            {
                exitTypeDescriptor = TypeDescriptor.F;
                var leftVal = leftNumber.ToSingle(CultureInfo.InvariantCulture);
                var rightVal = rightNumber.ToSingle(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal + rightVal);
            }
            else if (leftNumber is long || rightNumber is long)
            {
                exitTypeDescriptor = TypeDescriptor.J;
                var leftVal = leftNumber.ToInt64(CultureInfo.InvariantCulture);
                var rightVal = rightNumber.ToInt64(CultureInfo.InvariantCulture);
                return new TypedValue(leftVal + rightVal);
            }
            else if (CodeFlow.IsIntegerForNumericOp(leftNumber) || CodeFlow.IsIntegerForNumericOp(rightNumber))
            {
                exitTypeDescriptor = TypeDescriptor.I;
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
            exitTypeDescriptor = TypeDescriptor.String;
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

        return state.Operate(Operation.Add, leftOperand, rightOperand);
    }

    public override string ToStringAst()
    {
        if (children.Length < 2)
        {
            // unary plus
            return $"+{LeftOperand.ToStringAst()}";
        }

        return base.ToStringAst();
    }

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
        if (exitTypeDescriptor == TypeDescriptor.String)
        {
            gen.Emit(OpCodes.Newobj, StringBuilderConstructor);
            Walk(gen, cf, LeftOperand);
            Walk(gen, cf, RightOperand);
            gen.Emit(OpCodes.Callvirt, ToStringMethod);
        }
        else
        {
            children[0].GenerateCode(gen, cf);
            var leftDesc = children[0].ExitDescriptor;
            var exitDesc = exitTypeDescriptor;
            if (exitDesc == null)
            {
                throw new InvalidOperationException("No exit type descriptor");
            }

            CodeFlow.InsertNumericUnboxOrPrimitiveTypeCoercion(gen, leftDesc, exitDesc);
            if (children.Length > 1)
            {
                cf.EnterCompilationScope();
                children[1].GenerateCode(gen, cf);
                var rightDesc = children[1].ExitDescriptor;
                cf.ExitCompilationScope();
                CodeFlow.InsertNumericUnboxOrPrimitiveTypeCoercion(gen, rightDesc, exitDesc);
                gen.Emit(OpCodes.Add);
            }
        }

        cf.PushDescriptor(exitTypeDescriptor);
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
            if (cf.LastDescriptor() != TypeDescriptor.String)
            {
                gen.Emit(OpCodes.Castclass, typeof(string));
            }

            cf.ExitCompilationScope();
            gen.Emit(OpCodes.Callvirt, AppendStringMethod);
        }
    }
}
