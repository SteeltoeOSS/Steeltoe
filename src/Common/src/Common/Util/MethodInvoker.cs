// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Common.Util
{
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
            var result = 0;
            for (var i = 0; i < paramTypes.Length; i++)
            {
                if (!ClassUtils.IsAssignableValue(paramTypes[i], args[i]))
                {
                    return int.MaxValue;
                }

                if (args[i] != null)
                {
                    var paramType = paramTypes[i];
                    var superClass = args[i].GetType().BaseType;
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
                var lastDotIndex = StaticMethod.LastIndexOf('.');
                if (lastDotIndex == -1 || lastDotIndex == StaticMethod.Length)
                {
                    throw new ArgumentException(
                            "staticMethod must be a fully qualified class plus method name: " +
                            "e.g. 'example.MyExampleClass.myExampleMethod'");
                }

                var className = StaticMethod.Substring(0, lastDotIndex);
                var methodName = StaticMethod.Substring(lastDotIndex + 1);
                TargetClass = ResolveClassName(className);
                TargetMethod = methodName;
            }

            var targetClass = TargetClass;
            var targetMethod = TargetMethod;
            if (targetClass == null)
            {
                throw new ArgumentNullException("Either 'targetClass' or 'targetObject' is required");
            }

            if (targetMethod == null)
            {
                throw new ArgumentNullException("Property 'targetMethod' is required");
            }

            var arguments = Arguments;
            var argTypes = new Type[arguments.Length];
            for (var i = 0; i < arguments.Length; ++i)
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
            var targetObject = TargetObject;
            var preparedMethod = GetPreparedMethod();
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
            var targetMethod = TargetMethod;
            var arguments = Arguments;
            var argCount = arguments.Length;

            var targetClass = TargetClass;
            if (targetClass == null)
            {
                throw new InvalidOperationException("No target class set");
            }

            var candidates = targetClass.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            var minTypeDiffWeight = int.MaxValue;
            MethodInfo matchingMethod = null;

            foreach (var candidate in candidates)
            {
                if (candidate.Name.Equals(targetMethod) && candidate.GetParameters().Length == argCount)
                {
                    var paramTypes = GetParameterTypes(candidate);
                    var typeDiffWeight = GetTypeDifferenceWeight(paramTypes, arguments);
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
            var parameters = candidate.GetParameters();
            var result = new Type[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                result[i] = parameters[i].ParameterType;
            }

            return result;
        }
    }
}
