// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Steeltoe.Common.Util;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public abstract class Operator : SpelNode
{
    private static readonly ISet<TypeCode> NumericTypeCodes = new[]
    {
        TypeCode.Byte,
        TypeCode.Decimal,
        TypeCode.Double,
        TypeCode.Int16,
        TypeCode.Int32,
        TypeCode.Int64,
        TypeCode.SByte,
        TypeCode.Single,
        TypeCode.UInt16,
        TypeCode.UInt32,
        TypeCode.UInt64
    }.ToHashSet();

    protected static readonly MethodInfo EqualityCheckMethod = typeof(Operator).GetMethod("EqualityCheck", new[]
    {
        typeof(IEvaluationContext),
        typeof(object),
        typeof(object)
    });

    protected readonly string InnerOperatorName;

    // The descriptors of the runtime operand values are used if the discovered declared
    // descriptors are not providing enough information (for example a generic type
    // whose accessors seem to only be returning 'Object' - the actual descriptors may
    // indicate 'int')
    protected TypeDescriptor leftActualDescriptor;

    protected TypeDescriptor rightActualDescriptor;

    public virtual SpelNode LeftOperand => children[0];

    public virtual SpelNode RightOperand => children[1];

    public virtual string OperatorName => InnerOperatorName;

    protected Operator(string payload, int startPos, int endPos, params SpelNode[] operands)
        : base(startPos, endPos, operands)
    {
        InnerOperatorName = payload;
    }

    public static bool IsNumber(object target)
    {
        if (target is not IConvertible targetConv)
        {
            return false;
        }

        TypeCode typeCode = targetConv.GetTypeCode();
        return NumericTypeCodes.Contains(typeCode);
    }

    public static bool EqualityCheck(IEvaluationContext context, object left, object right)
    {
        if (IsNumber(left) && IsNumber(right))
        {
            var leftConv = (IConvertible)left;
            var rightConv = (IConvertible)right;

            if (left is decimal || right is decimal)
            {
                decimal leftVal = leftConv.ToDecimal(CultureInfo.InvariantCulture);
                decimal rightVal = rightConv.ToDecimal(CultureInfo.InvariantCulture);
                return leftVal == rightVal;
            }

            if (left is double || right is double)
            {
                double leftVal = leftConv.ToDouble(CultureInfo.InvariantCulture);
                double rightVal = rightConv.ToDouble(CultureInfo.InvariantCulture);
#pragma warning disable S1244 // Floating point numbers should not be tested for equality
                return leftVal == rightVal;
#pragma warning restore S1244 // Floating point numbers should not be tested for equality
            }

            if (left is float || right is float)
            {
                float leftVal = leftConv.ToSingle(CultureInfo.InvariantCulture);
                float rightVal = rightConv.ToSingle(CultureInfo.InvariantCulture);
#pragma warning disable S1244 // Floating point numbers should not be tested for equality
                return leftVal == rightVal;
#pragma warning restore S1244 // Floating point numbers should not be tested for equality
            }

            if (left is long || right is long)
            {
                long leftVal = leftConv.ToInt64(CultureInfo.InvariantCulture);
                long rightVal = rightConv.ToInt64(CultureInfo.InvariantCulture);
                return leftVal == rightVal;
            }

            if (left is int || right is int)
            {
                int leftVal = leftConv.ToInt32(CultureInfo.InvariantCulture);
                int rightVal = rightConv.ToInt32(CultureInfo.InvariantCulture);
                return leftVal == rightVal;
            }

            if (left is short || right is short)
            {
                short leftVal = leftConv.ToInt16(CultureInfo.InvariantCulture);
                short rightVal = rightConv.ToInt16(CultureInfo.InvariantCulture);
                return leftVal == rightVal;
            }

            if (left is byte || right is byte)
            {
                byte leftVal = leftConv.ToByte(CultureInfo.InvariantCulture);
                byte rightVal = rightConv.ToByte(CultureInfo.InvariantCulture);
                return leftVal == rightVal;
            }

            if (left is sbyte || right is sbyte)
            {
                sbyte leftVal = leftConv.ToSByte(CultureInfo.InvariantCulture);
                sbyte rightVal = rightConv.ToSByte(CultureInfo.InvariantCulture);
                return leftVal == rightVal;
            }

            if (left is uint || right is uint)
            {
                uint leftVal = leftConv.ToUInt32(CultureInfo.InvariantCulture);
                uint rightVal = rightConv.ToUInt32(CultureInfo.InvariantCulture);
                return leftVal == rightVal;
            }

            if (left is ushort || right is ushort)
            {
                ushort leftVal = leftConv.ToUInt16(CultureInfo.InvariantCulture);
                ushort rightVal = rightConv.ToUInt16(CultureInfo.InvariantCulture);
                return leftVal == rightVal;
            }

            if (left is ulong || right is ulong)
            {
                ulong leftVal = leftConv.ToUInt64(CultureInfo.InvariantCulture);
                ulong rightVal = rightConv.ToUInt64(CultureInfo.InvariantCulture);
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
            Type ancestor = ClassUtils.DetermineCommonAncestor(left.GetType(), right.GetType());

            if (ancestor != null && typeof(IComparable).IsAssignableFrom(ancestor))
            {
                return context.TypeComparator.Compare(left, right) == 0;
            }
        }

        return false;
    }

    public override string ToStringAst()
    {
        var sb = new StringBuilder("(");
        sb.Append(GetChild(0).ToStringAst());

        for (int i = 1; i < ChildCount; i++)
        {
            sb.Append(" ").Append(OperatorName).Append(" ");
            sb.Append(GetChild(i).ToStringAst());
        }

        sb.Append(")");
        return sb.ToString();
    }

    protected virtual bool IsCompilableOperatorUsingNumerics()
    {
        SpelNode left = LeftOperand;
        SpelNode right = RightOperand;

        if (!left.IsCompilable() || !right.IsCompilable())
        {
            return false;
        }

        // Supported operand types for equals (at the moment)
        TypeDescriptor leftDesc = left.ExitDescriptor;
        TypeDescriptor rightDesc = right.ExitDescriptor;
        DescriptorComparison dc = DescriptorComparison.CheckNumericCompatibility(leftDesc, rightDesc, leftActualDescriptor, rightActualDescriptor);
        return dc.AreNumbers && dc.AreCompatible;
    }

    protected void GenerateComparisonCode(ILGenerator gen, CodeFlow cf, OpCode brToElseInstruction)
    {
        SpelNode left = LeftOperand;
        SpelNode right = RightOperand;
        TypeDescriptor leftDesc = left.ExitDescriptor;
        TypeDescriptor rightDesc = right.ExitDescriptor;

        Label elseTarget = gen.DefineLabel();
        Label endOfIfTarget = gen.DefineLabel();

        bool unboxLeft = !CodeFlow.IsValueType(leftDesc);
        bool unboxRight = !CodeFlow.IsValueType(rightDesc);

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

        LocalBuilder leftLocal = gen.DeclareLocal(typeof(object));
        LocalBuilder rightLocal = gen.DeclareLocal(typeof(object));
        gen.Emit(OpCodes.Stloc, rightLocal);
        gen.Emit(OpCodes.Stloc, leftLocal);

        gen.Emit(OpCodes.Ldloc, leftLocal);
        gen.Emit(OpCodes.Ldloc, rightLocal);

        // This code block checks whether the left or right operand is null and handles
        // those cases before letting the original code (that only handled actual numbers) run
        Label rightIsNonNullTarget = gen.DefineLabel();

        // stack: left/right
        gen.Emit(OpCodes.Brtrue, rightIsNonNullTarget);

        // stack: left
        // here: RIGHT==null LEFT==unknown
        Label leftNotNullRightIsNullTarget = gen.DefineLabel();
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
            throw new InvalidOperationException($"Unsupported: {brToElseInstruction}");
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
            throw new InvalidOperationException($"Unsupported: {brToElseInstruction}");
        }

        gen.Emit(OpCodes.Br, endOfIfTarget);
        gen.MarkLabel(rightIsNonNullTarget);

        // stack: left
        // here: RIGHT!=null LEFT==unknown
        Label neitherRightNorLeftAreNullTarget = gen.DefineLabel();
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
            throw new InvalidOperationException($"Unsupported: {brToElseInstruction}");
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
        LocalBuilder result = gen.DeclareLocal(typeof(bool));
        gen.Emit(OpCodes.Stloc, result);
        gen.Emit(OpCodes.Ldloc, result);
        cf.PushDescriptor(TypeDescriptor.Z);
    }

    protected class DescriptorComparison
    {
        protected static readonly DescriptorComparison NotNumbers = new(false, false, TypeDescriptor.V);
        protected static readonly DescriptorComparison IncompatibleNumbers = new(true, false, TypeDescriptor.V);

        protected readonly bool InnerAreNumbers; // Were the two compared descriptor both for numbers?

        protected readonly bool InnerAreCompatible; // If they were numbers, were they compatible?

        protected readonly TypeDescriptor InnerCompatibleType; // When compatible, what is the descriptor of the common type

        public bool AreNumbers => InnerAreNumbers;

        public bool AreCompatible => InnerAreCompatible;

        public TypeDescriptor CompatibleType => InnerCompatibleType;

        public DescriptorComparison(bool areNumbers, bool areCompatible, TypeDescriptor compatibleType)
        {
            InnerAreNumbers = areNumbers;
            InnerAreCompatible = areCompatible;
            InnerCompatibleType = compatibleType;
        }

        public static DescriptorComparison CheckNumericCompatibility(TypeDescriptor leftDeclaredDescriptor, TypeDescriptor rightDeclaredDescriptor,
            TypeDescriptor leftActualDescriptor, TypeDescriptor rightActualDescriptor)
        {
            TypeDescriptor ld = leftDeclaredDescriptor;
            TypeDescriptor rd = rightDeclaredDescriptor;

            bool leftNumeric = CodeFlow.IsPrimitiveOrUnboxableSupportedNumberOrBoolean(ld);
            bool rightNumeric = CodeFlow.IsPrimitiveOrUnboxableSupportedNumberOrBoolean(rd);

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

                return IncompatibleNumbers;
            }

            return NotNumbers;
        }
    }
}
