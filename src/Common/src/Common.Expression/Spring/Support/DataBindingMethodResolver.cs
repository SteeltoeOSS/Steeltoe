// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class DataBindingMethodResolver : ReflectiveMethodResolver
{
    private DataBindingMethodResolver()
    {
    }

    public static DataBindingMethodResolver ForInstanceMethodInvocation()
    {
        return new DataBindingMethodResolver();
    }

    public override IMethodExecutor Resolve(IEvaluationContext context, object targetObject, string name, List<Type> argumentTypes)
    {
        if (targetObject is Type)
        {
            throw new ArgumentException($"{nameof(DataBindingMethodResolver)} does not support {typeof(Type)} targets.", nameof(targetObject));
        }

        return base.Resolve(context, targetObject, name, argumentTypes);
    }

    protected override bool IsCandidateForInvocation(MethodInfo method, Type targetClass)
    {
        if (method.IsStatic)
        {
            return false;
        }

        Type type = method.DeclaringType;
        return type != typeof(object) && type != typeof(Type);
    }
}
