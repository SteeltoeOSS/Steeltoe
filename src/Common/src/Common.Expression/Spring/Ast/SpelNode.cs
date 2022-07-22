// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Common;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Util;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public abstract class SpelNode : ISpelNode
{
    protected SpelNode[] _children = SpelNode.NO_CHILDREN;
    protected volatile TypeDescriptor _exitTypeDescriptor;
    private static readonly SpelNode[] NO_CHILDREN = new SpelNode[0];
    private readonly int _startPos;
    private readonly int _endPos;
    private SpelNode _parent;

    protected SpelNode(int startPos, int endPos, params SpelNode[] operands)
    {
        _startPos = startPos;
        _endPos = endPos;
        if (!ObjectUtils.IsEmpty(operands))
        {
            _children = operands;
            foreach (var operand in operands)
            {
                if (operand == null)
                {
                    throw new InvalidOperationException("Operand must not be null");
                }

                operand._parent = this;
            }
        }
    }

    public virtual TypeDescriptor ExitDescriptor => _exitTypeDescriptor;

    public virtual int StartPosition => _startPos;

    public virtual int EndPosition => _endPos;

    public virtual int ChildCount => _children.Length;

    public virtual bool IsCompilable() => false;

    public virtual object GetValue(ExpressionState state)
    {
        return GetValueInternal(state).Value;
    }

    public virtual ITypedValue GetTypedValue(ExpressionState state)
    {
        return GetValueInternal(state);
    }

    public virtual bool IsWritable(ExpressionState state)
    {
        return false;
    }

    public virtual void SetValue(ExpressionState state, object newValue)
    {
        throw new SpelEvaluationException(StartPosition, SpelMessage.SETVALUE_NOT_SUPPORTED, GetType());
    }

    public virtual ISpelNode GetChild(int index)
    {
        return _children[index];
    }

    public virtual Type GetObjectType(object obj)
    {
        if (obj == null)
        {
            return null;
        }

        return obj is Type type ? type : obj.GetType();
    }

    public virtual void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        throw new InvalidOperationException(GetType().FullName + " has no GenerateCode(..) method");
    }

    public abstract ITypedValue GetValueInternal(ExpressionState state);

    public abstract string ToStringAST();

    protected internal virtual bool NextChildIs(params Type[] classes)
    {
        if (_parent != null)
        {
            var peers = _parent._children;
            for (int i = 0, max = peers.Length; i < max; i++)
            {
                if (this == peers[i])
                {
                    if (i + 1 >= max)
                    {
                        return false;
                    }

                    var peerClass = peers[i + 1].GetType();
                    foreach (var desiredClass in classes)
                    {
                        if (peerClass == desiredClass)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }

        return false;
    }

    protected internal virtual T GetValue<T>(ExpressionState state)
    {
        return (T)GetValue(state, typeof(T));
    }

    protected internal virtual object GetValue(ExpressionState state, Type desiredReturnType)
    {
        return ExpressionUtils.ConvertTypedValue(state.EvaluationContext, GetValueInternal(state), desiredReturnType);
    }

    protected internal virtual IValueRef GetValueRef(ExpressionState state)
    {
        throw new SpelEvaluationException(StartPosition, SpelMessage.NOT_ASSIGNABLE, ToStringAST());
    }

    protected static void GenerateCodeForArguments(ILGenerator gen, CodeFlow cf, MethodBase member, SpelNode[] arguments)
    {
        var paramDescriptors = CodeFlow.ToDescriptors(member.GetParameterTypes());
        var isVarargs = member.IsVarArgs();

        if (isVarargs)
        {
            var childCount = arguments.Length;

            // The final parameter may or may not need packaging into an array, or nothing may
            // have been passed to satisfy the varargs and so something needs to be built.
            int p;

            // Fulfill all the parameter requirements except the last one
            for (p = 0; p < paramDescriptors.Length - 1; p++)
            {
                GenerateCodeForArgument(gen, cf, arguments[p], paramDescriptors[p]);
            }

            var lastChild = childCount == 0 ? null : arguments[childCount - 1];
            var arrayType = paramDescriptors[paramDescriptors.Length - 1];

            // Determine if the final passed argument is already suitably packaged in array
            // form to be passed to the method
            if (lastChild != null && arrayType.Equals(lastChild.ExitDescriptor))
            {
                GenerateCodeForArgument(gen, cf, lastChild, paramDescriptors[p]);
            }
            else
            {
                var arrElemType = arrayType.Value.GetElementType();
                var arrElemDesc = new TypeDescriptor(arrElemType);

                gen.Emit(OpCodes.Ldc_I4, childCount - p);
                gen.Emit(OpCodes.Newarr, arrElemType);

                // Package up the remaining arguments into the array
                var arrayindex = 0;
                while (p < childCount)
                {
                    var child = arguments[p];
                    gen.Emit(OpCodes.Dup);
                    gen.Emit(OpCodes.Ldc_I4, arrayindex++);
                    GenerateCodeForArgument(gen, cf, child, arrElemDesc);
                    gen.Emit(GetStelemInsn(arrElemType));
                    p++;
                }
            }
        }
        else
        {
            for (var i = 0; i < paramDescriptors.Length; i++)
            {
                GenerateCodeForArgument(gen, cf, arguments[i], paramDescriptors[i]);
            }
        }
    }

    protected static void GenerateCodeForArgument(ILGenerator gen, CodeFlow cf, SpelNode argument, TypeDescriptor paramDesc)
    {
        cf.EnterCompilationScope();
        argument.GenerateCode(gen, cf);
        var lastDesc = cf.LastDescriptor();
        if (lastDesc == null)
        {
            throw new InvalidOperationException("No last descriptor");
        }

        var valueTypeOnStack = CodeFlow.IsValueType(lastDesc);

        // Check if need to box it for the method reference?
        if (valueTypeOnStack && paramDesc.IsReferenceType)
        {
            CodeFlow.InsertBoxIfNecessary(gen, lastDesc);
        }
        else if (paramDesc.IsValueType && !paramDesc.IsBoxed && !valueTypeOnStack)
        {
            gen.Emit(OpCodes.Unbox_Any, paramDesc.Value);
        }
        else
        {
            // This would be unnecessary in the case of subtyping (e.g. method takes Number but Integer passed in)
            CodeFlow.InsertCastClass(gen, paramDesc);
        }

        cf.ExitCompilationScope();
    }

    protected static OpCode GetLdElemInsn(Type arrElemType)
    {
        return arrElemType switch
        {
            var t when t == typeof(sbyte) => OpCodes.Ldelem_I1,
            var t when t == typeof(byte) => OpCodes.Ldelem_U1,
            var t when t == typeof(short) || t == typeof(char) => OpCodes.Ldelem_I2,
            var t when t == typeof(ushort) => OpCodes.Ldelem_U2,
            var t when t == typeof(int) => OpCodes.Ldelem_I4,
            var t when t == typeof(uint) => OpCodes.Ldelem_U4,
            var t when t == typeof(long) || t == typeof(ulong) => OpCodes.Ldelem_I8,
            var t when t == typeof(float) => OpCodes.Ldelem_R4,
            var t when t == typeof(double) => OpCodes.Ldelem_R8,
            var t when t == typeof(IntPtr) || t == typeof(UIntPtr) => OpCodes.Ldelem_I,
            _ => OpCodes.Ldelem_Ref
        };
    }

    protected static OpCode GetStelemInsn(Type arrElemType)
    {
        return arrElemType switch
        {
            var t when t == typeof(sbyte) || t == typeof(byte) || t == typeof(bool) => OpCodes.Stelem_I1,
            var t when t == typeof(short) || t == typeof(ushort) || t == typeof(char) => OpCodes.Stelem_I2,
            var t when t == typeof(int) || t == typeof(uint) => OpCodes.Stelem_I4,
            var t when t == typeof(long) || t == typeof(ulong) => OpCodes.Stelem_I8,
            var t when t == typeof(float) => OpCodes.Stelem_R4,
            var t when t == typeof(double) => OpCodes.Stelem_R8,
            var t when t == typeof(IntPtr) || t == typeof(UIntPtr) => OpCodes.Stelem_I,
            _ => OpCodes.Stelem_Ref
        };
    }
}