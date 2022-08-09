// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Common.Util;

public class MethodInvoker
{
    public Type TargetClass { get; set; }

    public object TargetObject { get; private set; }

    public string TargetMethod { get; set; }

    public string StaticMethod { get; set; }

    public object[] Arguments { get; private set; }

    public MethodInfo MethodObject { get; set; }

    public bool IsPrepared => MethodObject != null;

    public static int GetTypeDifferenceWeight(Type[] paramTypes, object[] args)
    {
        int result = 0;

        for (int i = 0; i < paramTypes.Length; i++)
        {
            if (!ClassUtils.IsAssignableValue(paramTypes[i], args[i]))
            {
                return int.MaxValue;
            }

            if (args[i] != null)
            {
                Type paramType = paramTypes[i];
                Type superClass = args[i].GetType().BaseType;

                while (superClass != null)
                {
                    if (paramType.Equals(superClass))
                    {
                        result += 2;
                        superClass = null;
                    }
                    else if (ClassUtils.IsAssignable(paramType, superClass))
                    {
                        result += 2;
                        superClass = superClass.BaseType;
                    }
                    else
                    {
                        superClass = null;
                    }
                }

                if (paramType.IsInterface)
                {
                    result++;
                }
            }
        }

        return result;
    }

    public virtual void SetTargetObject(object target)
    {
        TargetObject = target;

        if (target != null)
        {
            TargetClass = target.GetType();
        }
    }

    public virtual void SetArguments(params object[] arguments)
    {
        Arguments = arguments;
    }

    public virtual void Prepare()
    {
        if (StaticMethod != null)
        {
            int lastDotIndex = StaticMethod.LastIndexOf('.');

            if (lastDotIndex == -1 || lastDotIndex == StaticMethod.Length)
            {
                throw new ArgumentException("staticMethod must be a fully qualified class plus method name: " +
                    "e.g. 'example.MyExampleClass.myExampleMethod'");
            }

            string className = StaticMethod.Substring(0, lastDotIndex);
            string methodName = StaticMethod.Substring(lastDotIndex + 1);
            TargetClass = ResolveClassName(className);
            TargetMethod = methodName;
        }

        Type targetClass = TargetClass;
        string targetMethod = TargetMethod;

        if (targetClass == null)
        {
            throw new InvalidOperationException($"{nameof(TargetClass)} must be set first.");
        }

        if (targetMethod == null)
        {
            throw new InvalidOperationException($"{nameof(TargetMethod)} must be set first.");
        }

        object[] arguments = Arguments;
        var argTypes = new Type[arguments.Length];

        for (int i = 0; i < arguments.Length; ++i)
        {
            argTypes[i] = arguments[i] != null ? arguments[i].GetType() : typeof(object);
        }

        try
        {
            MethodObject = targetClass.GetMethod(targetMethod, argTypes);
        }
        catch (Exception)
        {
            // Just rethrow exception if we can't get any match.
            MethodObject = FindMatchingMethod();

            if (MethodObject == null)
            {
                throw;
            }
        }
    }

    public object Invoke()
    {
        // In the static case, target will simply be {@code null}.
        object targetObject = TargetObject;
        MethodInfo preparedMethod = GetPreparedMethod();

        if (targetObject == null && !preparedMethod.IsStatic)
        {
            throw new InvalidOperationException("Target method must not be non-static without a target");
        }

        return preparedMethod.Invoke(targetObject, Arguments);
    }

    public MethodInfo GetPreparedMethod()
    {
        if (MethodObject == null)
        {
            throw new InvalidOperationException("Prepare() must be called prior to invoke() on MethodInvoker");
        }

        return MethodObject;
    }

    protected virtual MethodInfo FindMatchingMethod()
    {
        string targetMethod = TargetMethod;
        object[] arguments = Arguments;
        int argCount = arguments.Length;

        Type targetClass = TargetClass;

        if (targetClass == null)
        {
            throw new InvalidOperationException("No target class set");
        }

        MethodInfo[] candidates = targetClass.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        int minTypeDiffWeight = int.MaxValue;
        MethodInfo matchingMethod = null;

        foreach (MethodInfo candidate in candidates)
        {
            if (candidate.Name.Equals(targetMethod) && candidate.GetParameters().Length == argCount)
            {
                Type[] paramTypes = GetParameterTypes(candidate);
                int typeDiffWeight = GetTypeDifferenceWeight(paramTypes, arguments);

                if (typeDiffWeight < minTypeDiffWeight)
                {
                    minTypeDiffWeight = typeDiffWeight;
                    matchingMethod = candidate;
                }
            }
        }

        return matchingMethod;
    }

    protected virtual Type ResolveClassName(string className)
    {
        return Type.GetType(className, false);
    }

    private Type[] GetParameterTypes(MethodInfo candidate)
    {
        ParameterInfo[] parameters = candidate.GetParameters();
        var result = new Type[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            result[i] = parameters[i].ParameterType;
        }

        return result;
    }
}
