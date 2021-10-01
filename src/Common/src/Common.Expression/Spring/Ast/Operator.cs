// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public abstract class Operator : SpelNode
    {
        protected static readonly MethodInfo _equalityCheck = typeof(Operator).GetMethod(
            "EqualityCheck",
            new Type[] { typeof(IEvaluationContext), typeof(object), typeof(object) });

        protected readonly string _operatorName;

        // The descriptors of the runtime operand values are used if the discovered declared
        // descriptors are not providing enough information (for example a generic type
        // whose accessors seem to only be returning 'Object' - the actual descriptors may
        // indicate 'int')
        protected TypeDescriptor _leftActualDescriptor;

        protected TypeDescriptor _rightActualDescriptor;

        protected Operator(string payload, int startPos, int endPos, params SpelNode[] operands)
        : base(startPos, endPos, operands)
        {
            _operatorName = payload;
        }

        public virtual SpelNode LeftOperand => _children[0];

        public virtual SpelNode RightOperand => _children[1];

        public virtual string OperatorName => _operatorName;

        public static bool IsNumber(object target)
        {
            if (target is not IConvertible targetConv)
            {
                return false;
            }

            var tcode = targetConv.GetTypeCode();
            return tcode == TypeCode.Byte ||
                tcode == TypeCode.Decimal ||
                tcode == TypeCode.Double ||
                tcode == TypeCode.Int16 ||
                tcode == TypeCode.Int32 ||
                tcode == TypeCode.Int64 ||
                tcode == TypeCode.SByte ||
                tcode == TypeCode.Single ||
                tcode == TypeCode.UInt16 ||
                tcode == TypeCode.UInt32 ||
                tcode == TypeCode.UInt64;
        }

        public static bool EqualityCheck(IEvaluationContext context, object left, object right)
        {
            if (IsNumber(left) && IsNumber(right))
            {
                var leftConv = (IConvertible)left;
                var rightConv = (IConvertible)right;

                if (left is decimal || right is decimal)
                {
                    var leftVal = leftConv.ToDecimal(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToDecimal(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
                else if (left is double || right is double)
                {
                    var leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToDouble(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
                else if (left is float || right is float)
                {
                    var leftVal = leftConv.ToSingle(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToSingle(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
                else if (left is long || right is long)
                {
                    var leftVal = leftConv.ToInt64(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToInt64(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
                else if (left is int || right is int)
                {
                    var leftVal = leftConv.ToInt32(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToInt32(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
                else if (left is short || right is short)
                {
                    var leftVal = leftConv.ToInt16(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToInt16(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
                else if (left is byte || right is byte)
                {
                    var leftVal = leftConv.ToByte(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToByte(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
                else if (left is sbyte || right is sbyte)
                {
                    var leftVal = leftConv.ToSByte(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToSByte(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
                else if (left is uint || right is uint)
                {
                    var leftVal = leftConv.ToUInt32(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToUInt32(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
                else if (left is ushort || right is ushort)
                {
                    var leftVal = leftConv.ToUInt16(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToUInt16(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
                else if (left is ulong || right is ulong)
                {
                    var leftVal = leftConv.ToUInt64(CultureInfo.InvariantCulture);
                    var rightVal = rightConv.ToUInt64(CultureInfo.InvariantCulture);
                    return leftVal == rightVal;
                }
            }

            if (left is string && right is string)
            {
                return left.Equals(right);
            }

            if (left is bool && right is bool)
            {
                return left.Equals(right);
            }

            if (ObjectUtils.NullSafeEquals(left, right))
            {
                return true;
            }

            if (left is IComparable && right is IComparable)
            {
                var ancestor = ClassUtils.DetermineCommonAncestor(left.GetType(), right.GetType());
                if (ancestor != null && typeof(IComparable).IsAssignableFrom(ancestor))
                {
                    return context.TypeComparator.Compare(left, right) == 0;
                }
            }

            return false;
        }

        public override string ToStringAST()
        {
            var sb = new StringBuilder("(");
            sb.Append(GetChild(0).ToStringAST());
            for (var i = 1; i < ChildCount; i++)
            {
                sb.Append(" ").Append(OperatorName).Append(" ");
                sb.Append(GetChild(i).ToStringAST());
            }

            sb.Append(")");
            return sb.ToString();
        }

        protected virtual bool IsCompilableOperatorUsingNumerics()
        {
            var left = LeftOperand;
            var right = RightOperand;
            if (!left.IsCompilable() || !right.IsCompilable())
            {
                return false;
            }

            // Supported operand types for equals (at the moment)
            var leftDesc = left.ExitDescriptor;
            var rightDesc = right.ExitDescriptor;
            var dc = DescriptorComparison.CheckNumericCompatibility(leftDesc, rightDesc, _leftActualDescriptor, _rightActualDescriptor);
            return dc.AreNumbers && dc.AreCompatible;
        }

        protected void GenerateComparisonCode(ILGenerator gen, CodeFlow cf, OpCode brToElseInstruction)
        {
            var left = LeftOperand;
            var right = RightOperand;
            var leftDesc = left.ExitDescriptor;
            var rightDesc = right.ExitDescriptor;

            var elseTarget = gen.DefineLabel();
            var endOfIfTarget = gen.DefineLabel();

            var unboxLeft = !CodeFlow.IsValueType(leftDesc);
            var unboxRight = !CodeFlow.IsValueType(rightDesc);

            cf.EnterCompilationScope();
            left.GenerateCode(gen, cf);
            cf.ExitCompilationScope();
            if (CodeFlow.IsValueType(leftDesc))
            {
                gen.Emit(OpCodes.Box, leftDesc.Value);
                unboxLeft = true;
            }

            cf.EnterCompilationScope();
            right.GenerateCode(gen, cf);
            cf.ExitCompilationScope();
            if (CodeFlow.IsValueType(rightDesc))
            {
                gen.Emit(OpCodes.Box, rightDesc.Value);
                unboxRight = true;
            }

            var leftLocal = gen.DeclareLocal(typeof(object));
            var rightLocal = gen.DeclareLocal(typeof(object));
            gen.Emit(OpCodes.Stloc, rightLocal);
            gen.Emit(OpCodes.Stloc, leftLocal);

            gen.Emit(OpCodes.Ldloc, leftLocal);
            gen.Emit(OpCodes.Ldloc, rightLocal);

            // This code block checks whether the left or right operand is null and handles
            // those cases before letting the original code (that only handled actual numbers) run
            var rightIsNonNullTarget = gen.DefineLabel();

            // stack: left/right
            gen.Emit(OpCodes.Brtrue, rightIsNonNullTarget);

            // stack: left
            // here: RIGHT==null LEFT==unknown
            var leftNotNullRightIsNullTarget = gen.DefineLabel();
            gen.Emit(OpCodes.Brtrue, leftNotNullRightIsNullTarget);

            // stack: empty
            // here: RIGHT==null LEFT==null
            // load 0 or 1 depending on comparison instruction
            if (brToElseInstruction == OpCodes.Bge || brToElseInstruction == OpCodes.Ble)
            {
                gen.Emit(OpCodes.Ldc_I4_0);
            }
            else if (brToElseInstruction == OpCodes.Bgt || brToElseInstruction == OpCodes.Blt)
            {
                gen.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                throw new InvalidOperationException("Unsupported: " + brToElseInstruction);
            }

            gen.Emit(OpCodes.Br, endOfIfTarget);
            gen.MarkLabel(leftNotNullRightIsNullTarget);

            // stack: empty
            // RIGHT==null LEFT!=null
            // load 0 or 1 depending on comparison instruction
            if (brToElseInstruction == OpCodes.Bge || brToElseInstruction == OpCodes.Bgt)
            {
                gen.Emit(OpCodes.Ldc_I4_0);
            }
            else if (brToElseInstruction == OpCodes.Ble || brToElseInstruction == OpCodes.Blt)
            {
                gen.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                throw new InvalidOperationException("Unsupported: " + brToElseInstruction);
            }

            gen.Emit(OpCodes.Br, endOfIfTarget);
            gen.MarkLabel(rightIsNonNullTarget);

            // stack: left
            // here: RIGHT!=null LEFT==unknown
            var neitherRightNorLeftAreNullTarget = gen.DefineLabel();
            gen.Emit(OpCodes.Brtrue, neitherRightNorLeftAreNullTarget);

            // stack: empty
            // here: RIGHT!=null LEFT==null
            if (brToElseInstruction == OpCodes.Bge || brToElseInstruction == OpCodes.Bgt)
            {
                gen.Emit(OpCodes.Ldc_I4_1);
            }
            else if (brToElseInstruction == OpCodes.Ble || brToElseInstruction == OpCodes.Blt)
            {
                gen.Emit(OpCodes.Ldc_I4_0);
            }
            else
            {
                throw new InvalidOperationException("Unsupported: " + brToElseInstruction);
            }

            gen.Emit(OpCodes.Br, endOfIfTarget);
            gen.MarkLabel(neitherRightNorLeftAreNullTarget);

            // stack: empty
            // neither were null so unbox and proceed with numeric comparison
            gen.Emit(OpCodes.Ldloc, leftLocal);
            if (unboxLeft)
            {
                gen.Emit(OpCodes.Unbox_Any, leftDesc.Value);
            }

            // stack: left
            gen.Emit(OpCodes.Ldloc, rightLocal);
            if (unboxRight)
            {
                gen.Emit(OpCodes.Unbox_Any, rightDesc.Value);
            }

            // stack: left, right
            // Br instruction
            gen.Emit(brToElseInstruction, elseTarget);

            // Stack: Empty
            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Br, endOfIfTarget);
            gen.MarkLabel(elseTarget);

            // Stack: Empty
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.MarkLabel(endOfIfTarget);

            // Stack: result on stack, convert to bool
            var result = gen.DeclareLocal(typeof(bool));
            gen.Emit(OpCodes.Stloc, result);
            gen.Emit(OpCodes.Ldloc, result);
            cf.PushDescriptor(TypeDescriptor.Z);
        }

        protected class DescriptorComparison
        {
            protected static readonly DescriptorComparison NOT_NUMBERS = new(false, false, TypeDescriptor.V);
            protected static readonly DescriptorComparison INCOMPATIBLE_NUMBERS = new(true, false, TypeDescriptor.V);

            protected readonly bool _areNumbers;  // Were the two compared descriptor both for numbers?

            protected readonly bool _areCompatible;  // If they were numbers, were they compatible?

            protected readonly TypeDescriptor _compatibleType;  // When compatible, what is the descriptor of the common type

            public DescriptorComparison(bool areNumbers, bool areCompatible, TypeDescriptor compatibleType)
            {
                _areNumbers = areNumbers;
                _areCompatible = areCompatible;
                _compatibleType = compatibleType;
            }

            public bool AreNumbers => _areNumbers;

            public bool AreCompatible => _areCompatible;

            public TypeDescriptor CompatibleType => _compatibleType;

            public static DescriptorComparison CheckNumericCompatibility(TypeDescriptor leftDeclaredDescriptor, TypeDescriptor rightDeclaredDescriptor, TypeDescriptor leftActualDescriptor, TypeDescriptor rightActualDescriptor)
            {
                var ld = leftDeclaredDescriptor;
                var rd = rightDeclaredDescriptor;

                var leftNumeric = CodeFlow.IsPrimitiveOrUnboxableSupportedNumberOrBoolean(ld);
                var rightNumeric = CodeFlow.IsPrimitiveOrUnboxableSupportedNumberOrBoolean(rd);

                // If the declared descriptors aren't providing the information, try the actual descriptors
                if (!leftNumeric && !ObjectUtils.NullSafeEquals(ld, leftActualDescriptor))
                {
                    ld = leftActualDescriptor;
                    leftNumeric = CodeFlow.IsPrimitiveOrUnboxableSupportedNumberOrBoolean(ld);
                }

                if (!rightNumeric && !ObjectUtils.NullSafeEquals(rd, rightActualDescriptor))
                {
                    rd = rightActualDescriptor;
                    rightNumeric = CodeFlow.IsPrimitiveOrUnboxableSupportedNumberOrBoolean(rd);
                }

                if (leftNumeric && rightNumeric)
                {
                    if (CodeFlow.AreBoxingCompatible(ld, rd))
                    {
                        return new DescriptorComparison(true, true, CodeFlow.ToPrimitiveTargetDescriptor(ld));
                    }
                    else
                    {
                        return INCOMPATIBLE_NUMBERS;
                    }
                }
                else
                {
                    return NOT_NUMBERS;
                }
            }
        }
    }
}
