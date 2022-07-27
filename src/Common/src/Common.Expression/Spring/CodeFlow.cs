// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Standard;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring;

public class CodeFlow
{
    private static readonly Dictionary<Type, TypeDescriptor> _primitives = new ();
    private readonly CompiledExpression _compiledExpression;
    private readonly Stack<List<TypeDescriptor>> _compilationScopes;
    private readonly List<Action<ILGenerator, CodeFlow>> _initGenerators = new ();
    private int _nextFieldId = 1;

    static CodeFlow()
    {
        _primitives.Add(typeof(void), TypeDescriptor.V);
        _primitives.Add(typeof(int), TypeDescriptor.I);
        _primitives.Add(typeof(long), TypeDescriptor.J);
        _primitives.Add(typeof(float), TypeDescriptor.F);
        _primitives.Add(typeof(double), TypeDescriptor.D);
        _primitives.Add(typeof(byte), TypeDescriptor.B);
        _primitives.Add(typeof(char), TypeDescriptor.C);
        _primitives.Add(typeof(short), TypeDescriptor.S);
        _primitives.Add(typeof(bool), TypeDescriptor.Z);
        _primitives.Add(typeof(sbyte), TypeDescriptor.A);
        _primitives.Add(typeof(ushort), TypeDescriptor.M);
        _primitives.Add(typeof(uint), TypeDescriptor.N);
        _primitives.Add(typeof(ulong), TypeDescriptor.O);
        _primitives.Add(typeof(IntPtr), TypeDescriptor.P);
        _primitives.Add(typeof(UIntPtr), TypeDescriptor.Q);
    }

    public CodeFlow(CompiledExpression compiledExpression)
    {
        _compiledExpression = compiledExpression;
        _compilationScopes = new Stack<List<TypeDescriptor>>();
        _compilationScopes.Push(new List<TypeDescriptor>());
    }

    public static bool IsBooleanCompatible(TypeDescriptor descriptor)
    {
        return descriptor != null && (descriptor == TypeDescriptor.Z || descriptor == TypeDescriptor.Z.Boxed());
    }

    public static void InsertCastClass(ILGenerator gen, TypeDescriptor descriptor)
    {
        if (descriptor != null && !descriptor.IsPrimitive && !descriptor.IsValueType && descriptor != TypeDescriptor.OBJECT)
        {
            gen.Emit(OpCodes.Castclass, descriptor.Value);
        }
    }

    public static TypeDescriptor ToDescriptor(Type type)
    {
        if (IsPrimitive(type))
        {
            return _primitives[type];
        }

        return new TypeDescriptor(type);
    }

    public static TypeDescriptor ToDescriptorFromObject(object value)
    {
        if (value == null)
        {
            return TypeDescriptor.OBJECT;
        }

        var desc = ToDescriptor(value.GetType());

        if (desc.IsBoxable)
        {
            return desc.Boxed();
        }

        return desc;
    }

    public static bool IsPrimitive(Type type)
    {
        return type != null && (type.IsPrimitive || type == typeof(void));
    }

    public static bool IsValueType(TypeDescriptor descriptor)
    {
        return descriptor != null && descriptor.IsValueType && !descriptor.IsBoxed && !descriptor.IsVoid;
    }

    public static bool IsIntegerForNumericOp(object value)
    {
        var valueType = value?.GetType();

        // Other .NET types, what about long?
        return valueType == typeof(int) || valueType == typeof(short) || valueType == typeof(byte);
    }

    public static bool IsPrimitiveOrUnboxableSupportedNumberOrBoolean(TypeDescriptor descriptor)
    {
        if (descriptor == null)
        {
            return false;
        }

        if (IsPrimitiveOrUnboxableSupportedNumber(descriptor))
        {
            return true;
        }

        return descriptor == TypeDescriptor.Z || TypeDescriptor.Z.Boxed() == descriptor;
    }

    public static bool IsPrimitiveOrUnboxableSupportedNumber(TypeDescriptor descriptor)
    {
        if (descriptor == null)
        {
            return false;
        }

        if (descriptor.IsPrimitive)
        {
            return descriptor == TypeDescriptor.D || descriptor == TypeDescriptor.F || descriptor == TypeDescriptor.I || descriptor == TypeDescriptor.J;
        }

        return descriptor == TypeDescriptor.D.Boxed() || descriptor == TypeDescriptor.F.Boxed() || descriptor == TypeDescriptor.I.Boxed() || descriptor == TypeDescriptor.J.Boxed();
    }

    public static TypeDescriptor ToPrimitiveTargetDescriptor(TypeDescriptor descriptor)
    {
        if (descriptor.IsPrimitive)
        {
            return descriptor;
        }

        if (descriptor.IsBoxedPrimitive)
        {
            return descriptor.UnBox();
        }

        throw new InvalidOperationException("No primitive for '" + descriptor + "'");
    }

    public static void InsertBoxIfNecessary(ILGenerator gen, TypeDescriptor descriptor)
    {
        if (descriptor != null && descriptor.IsBoxable)
        {
            gen.Emit(OpCodes.Box, descriptor.Value);
        }
    }

    public static bool AreBoxingCompatible(TypeDescriptor desc1, TypeDescriptor desc2)
    {
        if (desc1 == desc2)
        {
            return true;
        }

        // Primitive and not boxed
        if (desc1.IsPrimitive)
        {
            return desc2 == desc1.Boxed();
        }
        else if (desc2.IsPrimitive)
        {
            return desc1 == desc2.Boxed();
        }

        return false;
    }

    public static void InsertNumericUnboxOrPrimitiveTypeCoercion(ILGenerator gen, TypeDescriptor stackDescriptor, TypeDescriptor targetDescriptor)
    {
        if (stackDescriptor.IsBoxedNumber)
        {
            gen.Emit(OpCodes.Unbox_Any, stackDescriptor.Value);
            CodeFlow.InsertAnyNecessaryTypeConversionBytecodes(gen, targetDescriptor, stackDescriptor.UnBox());
        }
        else
        {
            CodeFlow.InsertAnyNecessaryTypeConversionBytecodes(gen, targetDescriptor, stackDescriptor);
        }
    }

    public static void InsertAnyNecessaryTypeConversionBytecodes(ILGenerator gen, TypeDescriptor targetDescriptor, TypeDescriptor stackDescriptor)
    {
        if (CodeFlow.IsValueType(stackDescriptor))
        {
            if (stackDescriptor == TypeDescriptor.I || stackDescriptor == TypeDescriptor.B || stackDescriptor == TypeDescriptor.S || stackDescriptor == TypeDescriptor.C)
            {
                if (targetDescriptor == TypeDescriptor.D)
                {
                    gen.Emit(OpCodes.Conv_R8);
                }
                else if (targetDescriptor == TypeDescriptor.F)
                {
                    gen.Emit(OpCodes.Conv_R4);
                }
                else if (targetDescriptor == TypeDescriptor.J)
                {
                    if (stackDescriptor == TypeDescriptor.I || stackDescriptor == TypeDescriptor.S)
                    {
                        gen.Emit(OpCodes.Conv_I8);
                    }
                    else
                    {
                        gen.Emit(OpCodes.Conv_U8);
                    }
                }
                else if (targetDescriptor == TypeDescriptor.I)
                {
                    gen.Emit(OpCodes.Conv_I4);
                }
                else
                {
                    throw new InvalidOperationException("Cannot get from " + stackDescriptor + " to " + targetDescriptor);
                }
            }
            else if (stackDescriptor == TypeDescriptor.J)
            {
                if (targetDescriptor == TypeDescriptor.D)
                {
                    gen.Emit(OpCodes.Conv_R8);
                }
                else if (targetDescriptor == TypeDescriptor.F)
                {
                    gen.Emit(OpCodes.Conv_R4);
                }
                else if (targetDescriptor == TypeDescriptor.J)
                {
                    // nop
                }
                else if (targetDescriptor == TypeDescriptor.I)
                {
                    gen.Emit(OpCodes.Conv_I4);
                }
                else
                {
                    throw new InvalidOperationException("Cannot get from " + stackDescriptor + " to " + targetDescriptor);
                }
            }
            else if (stackDescriptor == TypeDescriptor.F)
            {
                if (targetDescriptor == TypeDescriptor.D)
                {
                    gen.Emit(OpCodes.Conv_R8);
                }
                else if (targetDescriptor == TypeDescriptor.F)
                {
                    // nop
                }
                else if (targetDescriptor == TypeDescriptor.J)
                {
                    gen.Emit(OpCodes.Conv_I8);
                }
                else if (targetDescriptor == TypeDescriptor.I)
                {
                    gen.Emit(OpCodes.Conv_I4);
                }
                else
                {
                    throw new InvalidOperationException("Cannot get from " + stackDescriptor + " to " + targetDescriptor);
                }
            }
            else if (stackDescriptor == TypeDescriptor.D)
            {
                if (targetDescriptor == TypeDescriptor.D)
                {
                    // nop
                }
                else if (targetDescriptor == TypeDescriptor.F)
                {
                    gen.Emit(OpCodes.Conv_R4);
                }
                else if (targetDescriptor == TypeDescriptor.J)
                {
                    gen.Emit(OpCodes.Conv_I8);
                }
                else if (targetDescriptor == TypeDescriptor.I)
                {
                    gen.Emit(OpCodes.Conv_I4);
                }
                else
                {
                    throw new InvalidOperationException("Cannot get from " + stackDescriptor + " to " + targetDescriptor);
                }
            }
        }
    }

    public static TypeDescriptor ToBoxedDescriptor(TypeDescriptor descriptor)
    {
        if (descriptor.IsBoxable)
        {
            return descriptor.Boxed();
        }

        return descriptor;
    }

    public static void LoadTarget(ILGenerator gen)
    {
        gen.Emit(OpCodes.Ldarg_1);
    }

    public static void LoadEvaluationContext(ILGenerator gen)
    {
        gen.Emit(OpCodes.Ldarg_2);
    }

    public static TypeDescriptor[] ToDescriptors(Type[] types)
    {
        var typesCount = types.Length;
        var descriptors = new TypeDescriptor[typesCount];
        for (var p = 0; p < typesCount; p++)
        {
            descriptors[p] = ToDescriptor(types[p]);
        }

        return descriptors;
    }

    public void UnboxBooleanIfNecessary(ILGenerator gen)
    {
        var lastDescriptor = LastDescriptor();
        if (lastDescriptor == TypeDescriptor.Z.Boxed())
        {
            gen.Emit(OpCodes.Unbox_Any, lastDescriptor.Value);
        }
    }

    public DynamicMethod Finish(int compilationId)
    {
        if (_initGenerators.Count > 0)
        {
            var methodName = "SpelExpressionInit" + compilationId;
            var method = new DynamicMethod(methodName, typeof(void), new Type[] { typeof(SpelCompiledExpression), typeof(object), typeof(IEvaluationContext) }, typeof(SpelCompiledExpression));
            var ilGenerator = method.GetILGenerator(4096);

            foreach (var generator in _initGenerators)
            {
                generator(ilGenerator, this);
            }

            ilGenerator.Emit(OpCodes.Ret);

            return method;
        }

        return null;
    }

    public void PushDescriptor(TypeDescriptor descriptor)
    {
        if (descriptor != null)
        {
            _compilationScopes.Peek().Add(descriptor);
        }
    }

    public void EnterCompilationScope()
    {
        _compilationScopes.Push(new List<TypeDescriptor>());
    }

    public void ExitCompilationScope()
    {
        _compilationScopes.Pop();
    }

    public TypeDescriptor LastDescriptor()
    {
        var top = _compilationScopes.Peek();
        if (top == null || top.Count == 0)
        {
            return null;
        }

        return top[top.Count - 1];
    }

    public int NextFieldId()
    {
        return _nextFieldId++;
    }

    public void RegisterNewInitGenerator(Action<ILGenerator, CodeFlow> generator)
    {
        _initGenerators.Add(generator);
    }

    public void RegisterNewField(string constantFieldName, object fieldValue)
    {
        _compiledExpression._dynamicFields.Add(constantFieldName, fieldValue);
    }
}