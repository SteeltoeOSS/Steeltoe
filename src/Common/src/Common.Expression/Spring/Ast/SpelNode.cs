// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Common;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public abstract class SpelNode : ISpelNode
    {
        protected SpelNode[] _children = SpelNode.NO_CHILDREN;
        protected volatile string _exitTypeDescriptor;
        private static readonly SpelNode[] NO_CHILDREN = new SpelNode[0];
        private readonly int _startPos;
        private readonly int _endPos;
        private SpelNode _parent;

        public SpelNode(int startPos, int endPos, params SpelNode[] operands)
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

        public virtual string ExitDescriptor => _exitTypeDescriptor;

        public virtual int StartPosition => _startPos;

        public virtual int EndPosition => _endPos;

        public virtual int ChildCount => _children.Length;

        public virtual bool IsCompilable() => false;

        public virtual object GetValue(ExpressionState expressionState)
        {
            return GetValueInternal(expressionState).Value;
        }

        public virtual ITypedValue GetTypedValue(ExpressionState expressionState)
        {
            return GetValueInternal(expressionState);
        }

        public virtual bool IsWritable(ExpressionState expressionState)
        {
            return false;
        }

        public virtual void SetValue(ExpressionState expressionState, object newValue)
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

            return obj is Type ? ((Type)obj) : obj.GetType();
        }

        public virtual void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // throw new InvalidOperationException(GetType().FullName + " has no GenerateCode(..) method");
        }

        public abstract ITypedValue GetValueInternal(ExpressionState expressionState);

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

        protected static void GenerateCodeForArguments(DynamicMethod mv, CodeFlow cf, MemberInfo member, SpelNode[] arguments)
        {
            // String[] paramDescriptors = null;
            // var isVarargs = false;
            // if (member is Constructor)
            // {
            //    var ctor = (Constructor)member;
            //    paramDescriptors = CodeFlow.ToDescriptors(ctor.getParameterTypes());
            //    isVarargs = ctor.isVarArgs();
            // }
            // else
            // {
            //    // Method
            //    var method = (MethodInfo)member;
            //    paramDescriptors = CodeFlow.ToDescriptors(method.getParameterTypes());
            //    isVarargs = method.isVarArgs();
            // }

            // if (isVarargs)
            // {
            //    // The final parameter may or may not need packaging into an array, or nothing may
            //    // have been passed to satisfy the varargs and so something needs to be built.
            //    var p = 0; // Current supplied argument being processed
            //    var childCount = arguments.Length;

            // // Fulfill all the parameter requirements except the last one
            //    for (p = 0; p < paramDescriptors.Length - 1; p++)
            //    {
            //        GenerateCodeForArgument(mv, cf, arguments[p], paramDescriptors[p]);
            //    }

            // var lastChild = (childCount == 0 ? null : arguments[childCount - 1]);
            //    var arrayType = paramDescriptors[paramDescriptors.Length - 1];
            //    // Determine if the final passed argument is already suitably packaged in array
            //    // form to be passed to the method
            //    if (lastChild != null && arrayType.equals(lastChild.getExitDescriptor()))
            //    {
            //        GenerateCodeForArgument(mv, cf, lastChild, paramDescriptors[p]);
            //    }
            //    else
            //    {
            //        arrayType = arrayType.Substring(1); // trim the leading '[', may leave other '['
            //                                            // build array big enough to hold remaining arguments
            //        CodeFlow.InsertNewArrayCode(mv, childCount - p, arrayType);
            //        // Package up the remaining arguments into the array
            //        var arrayindex = 0;
            //        while (p < childCount)
            //        {
            //            var child = arguments[p];
            //            mv.visitInsn(DUP);
            //            CodeFlow.InsertOptimalLoad(mv, arrayindex++);
            //            GenerateCodeForArgument(mv, cf, child, arrayType);
            //            CodeFlow.InsertArrayStore(mv, arrayType);
            //            p++;
            //        }
            //    }
            // }
            // else
            // {
            //    for (var i = 0; i < paramDescriptors.Length; i++)
            //    {
            //        GenerateCodeForArgument(mv, cf, arguments[i], paramDescriptors[i]);
            //    }
            // }
        }

        protected static void GenerateCodeForArgument(DynamicMethod mv, CodeFlow cf, SpelNode argument, string paramDesc)
        {
            // cf.EnterCompilationScope();
            // argument.GenerateCode(mv, cf);
            // String lastDesc = cf.lastDescriptor();
            // Assert.state(lastDesc != null, "No last descriptor");
            // bool primitiveOnStack = CodeFlow.isPrimitive(lastDesc);
            //// Check if need to box it for the method reference?
            // if (primitiveOnStack && paramDesc.charAt(0) == 'L')
            // {
            //    CodeFlow.insertBoxIfNecessary(mv, lastDesc.charAt(0));
            // }
            // else if (paramDesc.length() == 1 && !primitiveOnStack)
            // {
            //    CodeFlow.insertUnboxInsns(mv, paramDesc.charAt(0), lastDesc);
            // }
            // else if (!paramDesc.equals(lastDesc))
            // {
            //    // This would be unnecessary in the case of subtyping (e.g. method takes Number but Integer passed in)
            //    CodeFlow.insertCheckCast(mv, paramDesc);
            // }

            // cf.exitCompilationScope();
        }
    }
}
