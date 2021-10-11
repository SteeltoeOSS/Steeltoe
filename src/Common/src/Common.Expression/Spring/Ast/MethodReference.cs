// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class MethodReference : SpelNode
    {
        private readonly string _name;

        private readonly bool _nullSafe;

        private TypeDescriptor _originalPrimitiveExitTypeDescriptor;

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
            UpdateExitTypeDescriptor(result.Value);
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
            if (executorToCheck == null || executorToCheck.HasProxyTarget || executorToCheck.Get() is not ReflectiveMethodExecutor)
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
            if (!ReflectionHelper.IsPublic(clazz) && executor.GetPublicDeclaringClass() == null)
            {
                return false;
            }

            return true;
        }

        public override void GenerateCode(ILGenerator gen, CodeFlow cf)
        {
            var method = GetTargetMethodAndType(out var classType);
            if (method.IsStatic)
            {
                GenerateStaticMethodCode(gen, cf, method);
            }
            else
            {
                GenerateInstanceMethodCode(gen, cf, method, classType);
            }

            cf.PushDescriptor(_exitTypeDescriptor);
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

        protected internal TypeDescriptor ComputeExitDescriptor(object result, Type propertyReturnType)
        {
            if (propertyReturnType.IsValueType)
            {
                return CodeFlow.ToDescriptor(propertyReturnType);
            }

            return CodeFlow.ToDescriptorFromObject(result);
        }

        private void GenerateStaticMethodCode(ILGenerator gen, CodeFlow cf, MethodInfo method)
        {
            var stackDescriptor = cf.LastDescriptor();
            Label? skipIfNullTarget = null;
            if (_nullSafe)
            {
                skipIfNullTarget = GenerateNullCheckCode(gen);
            }

            if (stackDescriptor != null)
            {
                // Something on the stack when nothing is needed
                gen.Emit(OpCodes.Pop);
            }

            GenerateCodeForArguments(gen, cf, method, _children);
            gen.Emit(OpCodes.Call, method);

            if (_originalPrimitiveExitTypeDescriptor != null)
            {
                // The output of the accessor will be a primitive but from the block above it might be null,
                // so to have a 'common stack' element at skipIfNull target we need to box the primitive
                CodeFlow.InsertBoxIfNecessary(gen, _originalPrimitiveExitTypeDescriptor);
            }

            if (skipIfNullTarget.HasValue)
            {
                gen.MarkLabel(skipIfNullTarget.Value);
            }
        }

        private void GenerateInstanceMethodCode(ILGenerator gen, CodeFlow cf, MethodInfo targetMethod, Type targetType)
        {
            var stackDescriptor = cf.LastDescriptor();
            if (stackDescriptor == null)
            {
                // Nothing on the stack but something is needed
                CodeFlow.LoadTarget(gen);
                stackDescriptor = TypeDescriptor.OBJECT;
            }

            Label? skipIfNullTarget = null;
            if (_nullSafe)
            {
                skipIfNullTarget = GenerateNullCheckCode(gen);
            }

            if (targetType.IsValueType)
            {
                if (stackDescriptor.IsBoxed || stackDescriptor.IsReferenceType)
                {
                    gen.Emit(OpCodes.Unbox_Any, targetType);
                }

                var local = gen.DeclareLocal(targetType);
                gen.Emit(OpCodes.Stloc, local);
                gen.Emit(OpCodes.Ldloca, local);
            }
            else
            {
                if (stackDescriptor.Value != targetType)
                {
                    CodeFlow.InsertCastClass(gen, new TypeDescriptor(targetType));
                }
            }

            GenerateCodeForArguments(gen, cf, targetMethod, _children);
            if (targetType.IsValueType)
            {
                gen.Emit(OpCodes.Call, targetMethod);
            }
            else
            {
                gen.Emit(OpCodes.Callvirt, targetMethod);
            }

            if (_originalPrimitiveExitTypeDescriptor != null)
            {
                // The output of the accessor will be a primitive but from the block above it might be null,
                // so to have a 'common stack' element at skipIfNull target we need to box the primitive
                CodeFlow.InsertBoxIfNecessary(gen, _originalPrimitiveExitTypeDescriptor);
            }

            if (skipIfNullTarget.HasValue)
            {
                gen.MarkLabel(skipIfNullTarget.Value);
            }
        }

        private Label GenerateNullCheckCode(ILGenerator gen)
        {
            var skipIfNullTarget = gen.DefineLabel();
            var continueTarget = gen.DefineLabel();
            gen.Emit(OpCodes.Dup);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Cgt_Un);
            gen.Emit(OpCodes.Brtrue, continueTarget);

            // cast null on stack to result type
            CodeFlow.InsertCastClass(gen, _exitTypeDescriptor);
            gen.Emit(OpCodes.Br, skipIfNullTarget);
            gen.MarkLabel(continueTarget);
            return skipIfNullTarget;
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
            _cachedExecutor = new CachedMethodExecutor(executorToUse, value is Type type ? type : null, targetType, argumentTypes);
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
                if (rootCause is SystemException exception)
                {
                    throw exception;
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
                descriptors.Add(argument?.GetType());
            }

            return descriptors;
        }

        private IMethodExecutor GetCachedExecutor(IEvaluationContext evaluationContext, object value, Type target, IList<Type> argumentTypes)
        {
            var methodResolvers = evaluationContext.MethodResolvers;
            if (methodResolvers.Count != 1 || methodResolvers[0] is not ReflectiveMethodResolver)
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
            var className = FormatHelper.FormatClassNameForMessage(targetObject is Type type ? type : targetObject.GetType());
            if (accessException != null)
            {
                throw new SpelEvaluationException(StartPosition, accessException, SpelMessage.PROBLEM_LOCATING_METHOD, method, className);
            }
            else
            {
                throw new SpelEvaluationException(StartPosition, SpelMessage.METHOD_NOT_FOUND, method, className);
            }
        }

        private void UpdateExitTypeDescriptor(object result)
        {
            var executorToCheck = _cachedExecutor;
            if (executorToCheck != null && executorToCheck.Get() is ReflectiveMethodExecutor executor)
            {
                var method = executor.Method;
                var descriptor = ComputeExitDescriptor(result, method.ReturnType);
                if (_nullSafe && CodeFlow.IsValueType(descriptor))
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

        private MethodInfo GetTargetMethodAndType(out Type targetType)
        {
            var methodExecutor = (ReflectiveMethodExecutor)_cachedExecutor?.Get();
            if (methodExecutor == null)
            {
                throw new InvalidOperationException("No applicable cached executor found: " + _cachedExecutor);
            }

            var method = methodExecutor.Method;
            targetType = GetMethodTargetType(method, methodExecutor);
            return method;
        }

        private Type GetMethodTargetType(MethodInfo method, ReflectiveMethodExecutor methodExecutor)
        {
            if (ReflectionHelper.IsPublic(method.DeclaringType))
            {
                return method.DeclaringType;
            }
            else
            {
                return methodExecutor.GetPublicDeclaringClass();
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
                _mref.UpdateExitTypeDescriptor(result.Value);
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
                        ObjectUtils.NullSafeEquals(_target, target) && Equal(_argumentTypes, argumentTypes);
            }

            public bool HasProxyTarget => false;

            public IMethodExecutor Get()
            {
                return _methodExecutor;
            }

            private static bool Equal(IList<Type> list1, IList<Type> list2)
            {
                if (list1 == list2)
                {
                    return true;
                }

                if (list1 == null)
                {
                    return list2 == null;
                }

                if (list2 == null)
                {
                    return list1 == null;
                }

                if (list1.Count != list2.Count)
                {
                    return false;
                }

                for (var i = 0; i < list1.Count; i++)
                {
                    if (list1[i] != list2[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
