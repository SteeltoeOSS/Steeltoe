// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support
{
    public class ReflectiveMethodExecutor : IMethodExecutor
    {
        private readonly MethodInfo _originalMethod;

        private readonly MethodInfo _methodToInvoke;

        private readonly int? _varargsPosition;

        private bool _computedPublicDeclaringClass = false;

        private Type _publicDeclaringClass;

        private bool _argumentConversionOccurred = false;

        public ReflectiveMethodExecutor(MethodInfo method)
        {
            _originalMethod = method;
            _methodToInvoke = ClassUtils.GetInterfaceMethodIfPossible(method);
            if (method.IsVarArgs())
            {
                _varargsPosition = method.GetParameters().Length - 1;
            }
            else
            {
                _varargsPosition = null;
            }
        }

        public MethodInfo Method => _originalMethod;

        public Type GetPublicDeclaringClass()
        {
            if (!_computedPublicDeclaringClass)
            {
                _publicDeclaringClass = DiscoverPublicDeclaringClass(_originalMethod, _originalMethod.DeclaringType);
                _computedPublicDeclaringClass = true;
            }

            return _publicDeclaringClass;
        }

        private Type DiscoverPublicDeclaringClass(MethodInfo method, Type clazz)
        {
            if (ReflectionHelper.IsPublic(clazz))
            {
                try
                {
                    clazz.GetMethod(method.Name, ClassUtils.GetParameterTypes(method));
                    return clazz;
                }
                catch (Exception)
                {
                    // Continue below...
                }
            }

            if (clazz.BaseType != null)
            {
                return DiscoverPublicDeclaringClass(method, clazz.BaseType);
            }

            return null;
        }

        public bool DidArgumentConversionOccur => _argumentConversionOccurred;

        public ITypedValue Execute(IEvaluationContext context, object target, params object[] arguments)
        {
            try
            {
                _argumentConversionOccurred = ReflectionHelper.ConvertArguments(context.TypeConverter, arguments, _originalMethod, _varargsPosition);
                if (_originalMethod.IsVarArgs())
                {
                    arguments = ReflectionHelper.SetupArgumentsForVarargsInvocation(ClassUtils.GetParameterTypes(_originalMethod), arguments);
                }

                var value = _methodToInvoke.Invoke(target, arguments);
                return new TypedValue(value, value?.GetType() ?? _originalMethod.ReturnType);
            }
            catch (Exception ex)
            {
                throw new AccessException("Problem invoking method: " + _methodToInvoke, ex);
            }
        }
    }
}
