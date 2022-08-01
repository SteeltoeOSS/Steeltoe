// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class ReflectiveMethodExecutor : IMethodExecutor
{
    private readonly MethodInfo _methodToInvoke;

    private readonly int? _varargsPosition;

    private bool _computedPublicDeclaringClass;

    private Type _publicDeclaringClass;

    public ReflectiveMethodExecutor(MethodInfo method)
    {
        Method = method;
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

    public MethodInfo Method { get; }

    public Type GetPublicDeclaringClass()
    {
        if (!_computedPublicDeclaringClass)
        {
            _publicDeclaringClass = DiscoverPublicDeclaringClass(Method, Method.DeclaringType);
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

    public bool DidArgumentConversionOccur { get; private set; }

    public ITypedValue Execute(IEvaluationContext context, object target, params object[] arguments)
    {
        try
        {
            DidArgumentConversionOccur = ReflectionHelper.ConvertArguments(context.TypeConverter, arguments, Method, _varargsPosition);
            if (Method.IsVarArgs())
            {
                arguments = ReflectionHelper.SetupArgumentsForVarargsInvocation(ClassUtils.GetParameterTypes(Method), arguments);
            }

            var value = _methodToInvoke.Invoke(target, arguments);
            return new TypedValue(value, value?.GetType() ?? Method.ReturnType);
        }
        catch (Exception ex)
        {
            throw new AccessException($"Problem invoking method: {_methodToInvoke}", ex);
        }
    }
}
