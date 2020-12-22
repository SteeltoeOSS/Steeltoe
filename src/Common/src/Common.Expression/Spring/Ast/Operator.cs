// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public abstract class Operator : SpelNode
    {
        protected readonly string _operatorName;

        // The descriptors of the runtime operand values are used if the discovered declared
        // descriptors are not providing enough information (for example a generic type
        // whose accessors seem to only be returning 'Object' - the actual descriptors may
        // indicate 'int')
        protected string _leftActualDescriptor;

        protected string _rightActualDescriptor;

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
            var targetConv = target as IConvertible;
            if (targetConv == null)
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
            var leftDesc = _exitTypeDescriptor;
            var rightDesc = _exitTypeDescriptor;
            var dc = DescriptorComparison.CheckNumericCompatibility(leftDesc, rightDesc, _leftActualDescriptor, _rightActualDescriptor);
            return dc.AreNumbers && dc.AreCompatible;
        }

        protected void GenerateComparisonCode(DynamicMethod mv, CodeFlow cf, int compInstruction1, int compInstruction2)
        {
            // SpelNodeImpl left = getLeftOperand();
            //    SpelNodeImpl right = getRightOperand();
            //    String leftDesc = left.exitTypeDescriptor;
            //    String rightDesc = right.exitTypeDescriptor;
            //    Label elseTarget = new Label();
            //    Label endOfIf = new Label();
            //    boolean unboxLeft = !CodeFlow.isPrimitive(leftDesc);
            //    boolean unboxRight = !CodeFlow.isPrimitive(rightDesc);
            //    DescriptorComparison dc = DescriptorComparison.checkNumericCompatibility(
            //            leftDesc, rightDesc, this.leftActualDescriptor, this.rightActualDescriptor);
            //    char targetType = dc.compatibleType;  // CodeFlow.toPrimitiveTargetDesc(leftDesc);

            // cf.enterCompilationScope();
            //    left.generateCode(mv, cf);
            //    cf.exitCompilationScope();
            //    if (CodeFlow.isPrimitive(leftDesc))
            //    {
            //        CodeFlow.insertBoxIfNecessary(mv, leftDesc);
            //        unboxLeft = true;
            //    }

            // cf.enterCompilationScope();
            //    right.generateCode(mv, cf);
            //    cf.exitCompilationScope();
            //    if (CodeFlow.isPrimitive(rightDesc))
            //    {
            //        CodeFlow.insertBoxIfNecessary(mv, rightDesc);
            //        unboxRight = true;
            //    }

            // // This code block checks whether the left or right operand is null and handles
            //    // those cases before letting the original code (that only handled actual numbers) run
            //    Label rightIsNonNull = new Label();
            //    mv.visitInsn(DUP);  // stack: left/right/right
            //    mv.visitJumpInsn(IFNONNULL, rightIsNonNull);  // stack: left/right
            //                                                  // here: RIGHT==null LEFT==unknown
            //    mv.visitInsn(SWAP);  // right/left
            //    Label leftNotNullRightIsNull = new Label();
            //    mv.visitJumpInsn(IFNONNULL, leftNotNullRightIsNull);  // stack: right
            //                                                          // here: RIGHT==null LEFT==null
            //    mv.visitInsn(POP);  // stack: <nothing>
            //                        // load 0 or 1 depending on comparison instruction
            //    switch (compInstruction1)
            //    {
            //        case IFGE: // OpLT
            //        case IFLE: // OpGT
            //            mv.visitInsn(ICONST_0);  // false - null is not < or > null
            //            break;
            //        case IFGT: // OpLE
            //        case IFLT: // OpGE
            //            mv.visitInsn(ICONST_1);  // true - null is <= or >= null
            //            break;
            //        default:
            //            throw new IllegalStateException("Unsupported: " + compInstruction1);
            //    }
            //    mv.visitJumpInsn(GOTO, endOfIf);
            //    mv.visitLabel(leftNotNullRightIsNull);  // stack: right
            //                                            // RIGHT==null LEFT!=null
            //    mv.visitInsn(POP);  // stack: <nothing>
            //                        // load 0 or 1 depending on comparison instruction
            //    switch (compInstruction1)
            //    {
            //        case IFGE: // OpLT
            //        case IFGT: // OpLE
            //            mv.visitInsn(ICONST_0);  // false - something is not < or <= null
            //            break;
            //        case IFLE: // OpGT
            //        case IFLT: // OpGE
            //            mv.visitInsn(ICONST_1);  // true - something is > or >= null
            //            break;
            //        default:
            //            throw new IllegalStateException("Unsupported: " + compInstruction1);
            //    }
            //    mv.visitJumpInsn(GOTO, endOfIf);

            // mv.visitLabel(rightIsNonNull);  // stack: left/right
            //                                    // here: RIGHT!=null LEFT==unknown
            //    mv.visitInsn(SWAP);  // stack: right/left
            //    mv.visitInsn(DUP);  // stack: right/left/left
            //    Label neitherRightNorLeftAreNull = new Label();
            //    mv.visitJumpInsn(IFNONNULL, neitherRightNorLeftAreNull);  // stack: right/left
            //                                                              // here: RIGHT!=null LEFT==null
            //    mv.visitInsn(POP2);  // stack: <nothing>
            //    switch (compInstruction1)
            //    {
            //        case IFGE: // OpLT
            //        case IFGT: // OpLE
            //            mv.visitInsn(ICONST_1);  // true - null is < or <= something
            //            break;
            //        case IFLE: // OpGT
            //        case IFLT: // OpGE
            //            mv.visitInsn(ICONST_0);  // false - null is not > or >= something
            //            break;
            //        default:
            //            throw new IllegalStateException("Unsupported: " + compInstruction1);
            //    }
            //    mv.visitJumpInsn(GOTO, endOfIf);
            //    mv.visitLabel(neitherRightNorLeftAreNull);  // stack: right/left
            //                                                // neither were null so unbox and proceed with numeric comparison
            //    if (unboxLeft)
            //    {
            //        CodeFlow.insertUnboxInsns(mv, targetType, leftDesc);
            //    }
            //    // What we just unboxed might be a double slot item (long/double)
            //    // so can't just use SWAP
            //    // stack: right/left(1or2slots)
            //    if (targetType == 'D' || targetType == 'J')
            //    {
            //        mv.visitInsn(DUP2_X1);
            //        mv.visitInsn(POP2);
            //    }
            //    else
            //    {
            //        mv.visitInsn(SWAP);
            //    }
            //    // stack: left(1or2)/right
            //    if (unboxRight)
            //    {
            //        CodeFlow.insertUnboxInsns(mv, targetType, rightDesc);
            //    }

            // // assert: SpelCompiler.boxingCompatible(leftDesc, rightDesc)
            //    if (targetType == 'D')
            //    {
            //        mv.visitInsn(DCMPG);
            //        mv.visitJumpInsn(compInstruction1, elseTarget);
            //    }
            //    else if (targetType == 'F')
            //    {
            //        mv.visitInsn(FCMPG);
            //        mv.visitJumpInsn(compInstruction1, elseTarget);
            //    }
            //    else if (targetType == 'J')
            //    {
            //        mv.visitInsn(LCMP);
            //        mv.visitJumpInsn(compInstruction1, elseTarget);
            //    }
            //    else if (targetType == 'I')
            //    {
            //        mv.visitJumpInsn(compInstruction2, elseTarget);
            //    }
            //    else
            //    {
            //        throw new IllegalStateException("Unexpected descriptor " + leftDesc);
            //    }

            // // Other numbers are not yet supported (isCompilable will not have returned true)
            //    mv.visitInsn(ICONST_1);
            //    mv.visitJumpInsn(GOTO, endOfIf);
            //    mv.visitLabel(elseTarget);
            //    mv.visitInsn(ICONST_0);
            //    mv.visitLabel(endOfIf);
            //    cf.pushDescriptor("Z");
        }

        protected class DescriptorComparison
        {
            protected static readonly DescriptorComparison _NOT_NUMBERS = new DescriptorComparison(false, false, " ");
            protected static readonly DescriptorComparison _INCOMPATIBLE_NUMBERS = new DescriptorComparison(true, false, " ");

            protected readonly bool _areNumbers;  // Were the two compared descriptor both for numbers?

            protected readonly bool _areCompatible;  // If they were numbers, were they compatible?

            protected readonly string _compatibleType;  // When compatible, what is the descriptor of the common type

            public DescriptorComparison(bool areNumbers, bool areCompatible, string compatibleType)
            {
                _areNumbers = areNumbers;
                _areCompatible = areCompatible;
                _compatibleType = compatibleType;
            }

            public bool AreNumbers => _areNumbers;

            public bool AreCompatible => _areCompatible;

            public static DescriptorComparison CheckNumericCompatibility(string leftDeclaredDescriptor, string rightDeclaredDescriptor, string leftActualDescriptor, string rightActualDescriptor)
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
                        return new DescriptorComparison(true, true, CodeFlow.ToPrimitiveTargetDesc(ld));
                    }
                    else
                    {
                        return _INCOMPATIBLE_NUMBERS;
                    }
                }
                else
                {
                    return _NOT_NUMBERS;
                }
            }
        }
    }
}
