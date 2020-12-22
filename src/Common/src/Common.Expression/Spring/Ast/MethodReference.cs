// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Spring.Support;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class MethodReference : SpelNode
    {
        private readonly string _name;

        private readonly bool _nullSafe;

        private string _originalPrimitiveExitTypeDescriptor;

        private volatile CachedMethodExecutor _cachedExecutor;

        public MethodReference(bool nullSafe, string methodName, int startPos, int endPos, params SpelNode[] arguments)
        : base(startPos, endPos, arguments)
        {
            _name = methodName;
            _nullSafe = nullSafe;
        }

        public string Name => _name;

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var evaluationContext = state.EvaluationContext;
            var value = state.GetActiveContextObject().Value;
            var targetType = state.GetActiveContextObject().TypeDescriptor;
            var arguments = GetArguments(state);
            var result = GetValueInternal(evaluationContext, value, targetType, arguments);
            UpdateExitTypeDescriptor();
            return result;
        }

        public override string ToStringAST()
        {
            var sj = new List<string>();
            for (var i = 0; i < ChildCount; i++)
            {
                sj.Add(GetChild(i).ToStringAST());
            }

            return _name + "(" + string.Join(",", sj) + ")";
        }

        public override bool IsCompilable()
        {
            var executorToCheck = _cachedExecutor;
            if (executorToCheck == null || executorToCheck.HasProxyTarget() || !(executorToCheck.Get() is ReflectiveMethodExecutor))
            {
                return false;
            }

            foreach (var child in _children)
            {
                if (!child.IsCompilable())
                {
                    return false;
                }
            }

            var executor = (ReflectiveMethodExecutor)executorToCheck.Get();
            if (executor.DidArgumentConversionOccur)
            {
                return false;
            }

            var clazz = executor.Method.DeclaringType;
            if (!clazz.IsPublic && executor.GetPublicDeclaringClass() == null)
            {
                return false;
            }

            return true;
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // CachedMethodExecutor executorToCheck = this.cachedExecutor;
            // if (executorToCheck == null || !(executorToCheck.get() is ReflectiveMethodExecutor))
            // {
            //    throw new IllegalStateException("No applicable cached executor found: " + executorToCheck);
            // }

            // ReflectiveMethodExecutor methodExecutor = (ReflectiveMethodExecutor)executorToCheck.get();
            // Method method = methodExecutor.getMethod();
            // boolean isStaticMethod = Modifier.isStatic(method.getModifiers());
            // String descriptor = cf.lastDescriptor();

            // Label skipIfNull = null;
            // if (descriptor == null && !isStaticMethod)
            // {
            //    // Nothing on the stack but something is needed
            //    cf.loadTarget(mv);
            // }
            // if ((descriptor != null || !isStaticMethod) && this.nullSafe)
            // {
            //    mv.visitInsn(DUP);
            //    skipIfNull = new Label();
            //    Label continueLabel = new Label();
            //    mv.visitJumpInsn(IFNONNULL, continueLabel);
            //    CodeFlow.insertCheckCast(mv, this.exitTypeDescriptor);
            //    mv.visitJumpInsn(GOTO, skipIfNull);
            //    mv.visitLabel(continueLabel);
            // }
            // if (descriptor != null && isStaticMethod)
            // {
            //    // Something on the stack when nothing is needed
            //    mv.visitInsn(POP);
            // }

            // if (CodeFlow.isPrimitive(descriptor))
            // {
            //    CodeFlow.insertBoxIfNecessary(mv, descriptor.charAt(0));
            // }

            // String classDesc;
            // if (Modifier.isPublic(method.getDeclaringClass().getModifiers()))
            // {
            //    classDesc = method.getDeclaringClass().getName().replace('.', '/');
            // }
            // else
            // {
            //    Type publicDeclaringClass = methodExecutor.getPublicDeclaringClass();
            //    Assert.state(publicDeclaringClass != null, "No public declaring class");
            //    classDesc = publicDeclaringClass.getName().replace('.', '/');
            // }

            // if (!isStaticMethod && (descriptor == null || !descriptor.substring(1).equals(classDesc)))
            // {
            //    CodeFlow.insertCheckCast(mv, "L" + classDesc);
            // }

            // GenerateCodeForArguments(mv, cf, method, this.children);
            // mv.visitMethodInsn((isStaticMethod ? INVOKESTATIC : (method.isDefault() ? INVOKEINTERFACE : INVOKEVIRTUAL)),
            //        classDesc, method.getName(), CodeFlow.createSignatureDescriptor(method),
            //        method.getDeclaringClass().isInterface());
            // cf.pushDescriptor(this.exitTypeDescriptor);

            // if (this.originalPrimitiveExitTypeDescriptor != null)
            // {
            //    // The output of the accessor will be a primitive but from the block above it might be null,
            //    // so to have a 'common stack' element at skipIfNull target we need to box the primitive
            //    CodeFlow.insertBoxIfNecessary(mv, this.originalPrimitiveExitTypeDescriptor);
            // }
            // if (skipIfNull != null)
            // {
            //    mv.visitLabel(skipIfNull);
            // }
        }

        protected internal override IValueRef GetValueRef(ExpressionState state)
        {
            var arguments = GetArguments(state);
            if (state.GetActiveContextObject().Value == null)
            {
                ThrowIfNotNullSafe(GetArgumentTypes(arguments));
                return NullValueRef.INSTANCE;
            }

            return new MethodValueRef(this, state, arguments);
        }

        private ITypedValue GetValueInternal(IEvaluationContext evaluationContext, object value, Type targetType, object[] arguments)
        {
            var argumentTypes = GetArgumentTypes(arguments);
            if (value == null)
            {
                ThrowIfNotNullSafe(argumentTypes);
                return TypedValue.NULL;
            }

            var executorToUse = GetCachedExecutor(evaluationContext, value, targetType, argumentTypes);
            if (executorToUse != null)
            {
                try
                {
                    return executorToUse.Execute(evaluationContext, value, arguments);
                }
                catch (AccessException ex)
                {
                    // Two reasons this can occur:
                    // 1. the method invoked actually threw a real exception
                    // 2. the method invoked was not passed the arguments it expected and
                    //    has become 'stale'

                    // In the first case we should not retry, in the second case we should see
                    // if there is a better suited method.

                    // To determine the situation, the AccessException will contain a cause.
                    // If the cause is an InvocationTargetException, a user exception was
                    // thrown inside the method. Otherwise the method could not be invoked.
                    ThrowSimpleExceptionIfPossible(value, ex);

                    // At this point we know it wasn't a user problem so worth a retry if a
                    // better candidate can be found.
                    _cachedExecutor = null;
                }
            }

            // either there was no accessor or it no longer existed
            executorToUse = FindAccessorForMethod(argumentTypes, value, evaluationContext);
            _cachedExecutor = new CachedMethodExecutor(executorToUse, value is Type ? (Type)value : null, targetType, argumentTypes);
            try
            {
                return executorToUse.Execute(evaluationContext, value, arguments);
            }
            catch (AccessException ex)
            {
                // Same unwrapping exception handling as above in above catch block
                ThrowSimpleExceptionIfPossible(value, ex);
                throw new SpelEvaluationException(StartPosition, ex, SpelMessage.EXCEPTION_DURING_METHOD_INVOCATION, _name, value.GetType().FullName, ex.Message);
            }
        }

        private void ThrowIfNotNullSafe(IList<Type> argumentTypes)
        {
            if (!_nullSafe)
            {
                throw new SpelEvaluationException(StartPosition, SpelMessage.METHOD_CALL_ON_NULL_OBJECT_NOT_ALLOWED, FormatHelper.FormatMethodForMessage(_name, argumentTypes));
            }
        }

        private void ThrowSimpleExceptionIfPossible(object value, AccessException ex)
        {
            if (ex.InnerException is TargetInvocationException)
            {
                var rootCause = ex.InnerException.InnerException;
                if (rootCause is SystemException)
                {
                    throw (SystemException)rootCause;
                }

                throw new ExpressionInvocationTargetException(StartPosition, "A problem occurred when trying to execute method '" + _name + "' on object of type [" + value.GetType().FullName + "]", rootCause);
            }
        }

        private object[] GetArguments(ExpressionState state)
        {
            var arguments = new object[ChildCount];
            for (var i = 0; i < arguments.Length; i++)
            {
                // Make the root object the active context again for evaluating the parameter expressions
                try
                {
                    state.PushActiveContextObject(state.GetScopeRootContextObject());
                    arguments[i] = _children[i].GetValueInternal(state).Value;
                }
                finally
                {
                    state.PopActiveContextObject();
                }
            }

            return arguments;
        }

        private List<Type> GetArgumentTypes(params object[] arguments)
        {
            var descriptors = new List<Type>(arguments.Length);
            foreach (var argument in arguments)
            {
                descriptors.Add(argument == null ? typeof(object) : argument.GetType());
            }

            return descriptors;
        }

        private IMethodExecutor GetCachedExecutor(IEvaluationContext evaluationContext, object value, Type target, IList<Type> argumentTypes)
        {
            var methodResolvers = evaluationContext.MethodResolvers;
            if (methodResolvers.Count != 1 || !(methodResolvers[0] is ReflectiveMethodResolver))
            {
                // Not a default ReflectiveMethodResolver - don't know whether caching is valid
                return null;
            }

            var executorToCheck = _cachedExecutor;
            if (executorToCheck != null && executorToCheck.IsSuitable(value, target, argumentTypes))
            {
                return executorToCheck.Get();
            }

            _cachedExecutor = null;
            return null;
        }

        private IMethodExecutor FindAccessorForMethod(List<Type> argumentTypes, object targetObject, IEvaluationContext evaluationContext)
        {
            AccessException accessException = null;
            var methodResolvers = evaluationContext.MethodResolvers;
            foreach (var methodResolver in methodResolvers)
            {
                try
                {
                    var methodExecutor = methodResolver.Resolve(evaluationContext, targetObject, _name, argumentTypes);
                    if (methodExecutor != null)
                    {
                        return methodExecutor;
                    }
                }
                catch (AccessException ex)
                {
                    accessException = ex;
                    break;
                }
            }

            var method = FormatHelper.FormatMethodForMessage(_name, argumentTypes);
            var className = FormatHelper.FormatClassNameForMessage(targetObject is Type ? ((Type)targetObject) : targetObject.GetType());
            if (accessException != null)
            {
                throw new SpelEvaluationException(StartPosition, accessException, SpelMessage.PROBLEM_LOCATING_METHOD, method, className);
            }
            else
            {
                throw new SpelEvaluationException(StartPosition, SpelMessage.METHOD_NOT_FOUND, method, className);
            }
        }

        private void UpdateExitTypeDescriptor()
        {
            var executorToCheck = _cachedExecutor;
            if (executorToCheck != null && executorToCheck.Get() is ReflectiveMethodExecutor)
            {
                var method = ((ReflectiveMethodExecutor)executorToCheck.Get()).Method;
                var descriptor = CodeFlow.ToDescriptor(method.ReturnType);
                if (_nullSafe && CodeFlow.IsPrimitive(descriptor))
                {
                    _originalPrimitiveExitTypeDescriptor = descriptor;
                    _exitTypeDescriptor = CodeFlow.ToBoxedDescriptor(descriptor);
                }
                else
                {
                    _exitTypeDescriptor = descriptor;
                }
            }
        }

        private class MethodValueRef : IValueRef
        {
            private readonly IEvaluationContext _evaluationContext;
            private readonly object _value;
            private readonly Type _targetType;
            private readonly object[] _arguments;
            private readonly MethodReference _mref;

            public MethodValueRef(MethodReference mref, ExpressionState state, object[] arguments)
            {
                _mref = mref;
                _evaluationContext = state.EvaluationContext;
                _value = state.GetActiveContextObject().Value;
                _targetType = state.GetActiveContextObject().TypeDescriptor;
                _arguments = arguments;
            }

            public ITypedValue GetValue()
            {
                var result = _mref.GetValueInternal(_evaluationContext, _value, _targetType, _arguments);
                _mref.UpdateExitTypeDescriptor();
                return result;
            }

            public void SetValue(object newValue)
            {
                throw new InvalidOperationException();
            }

            public bool IsWritable => false;
        }

        private class CachedMethodExecutor
        {
            private readonly IMethodExecutor _methodExecutor;
            private readonly Type _staticClass;
            private readonly Type _target;
            private readonly List<Type> _argumentTypes;

            public CachedMethodExecutor(IMethodExecutor methodExecutor, Type staticClass, Type target, List<Type> argumentTypes)
            {
                _methodExecutor = methodExecutor;
                _staticClass = staticClass;
                _target = target;
                _argumentTypes = argumentTypes;
            }

            public bool IsSuitable(object value, Type target, IList<Type> argumentTypes)
            {
                return (_staticClass == null || _staticClass.Equals(value)) &&
                        ObjectUtils.NullSafeEquals(_target, target) && _argumentTypes.Equals(argumentTypes);
            }

#pragma warning disable S3400 // Methods should not return constants
            public bool HasProxyTarget()
#pragma warning restore S3400 // Methods should not return constants
            {
                // return _target != null && Proxy.IsProxyClass(_target);
                return false;
            }

            public IMethodExecutor Get()
            {
                return _methodExecutor;
            }
        }
    }
}
