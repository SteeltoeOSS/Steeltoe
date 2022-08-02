// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;

namespace Steeltoe.Stream.Util;

internal static class GenericsUtils
{
    internal static Type GetParameterType(Type evaluatedClass, Type interfaceClass, int position)
    {
        Type bindableType = null;

        if (!interfaceClass.IsInterface)
        {
            throw new ArgumentException($"{nameof(interfaceClass)} is not an interface");
        }

        Type currentType = evaluatedClass;

        while (!typeof(object).Equals(currentType) && bindableType == null)
        {
            Type[] interfaces = currentType.GetInterfaces();
            Type resolvableType = null;

            foreach (Type interfaceType in interfaces)
            {
                Type typeToCheck = interfaceType;

                if (interfaceType.IsGenericType)
                {
                    typeToCheck = interfaceType.GetGenericTypeDefinition();
                }

                if (interfaceClass == typeToCheck)
                {
                    resolvableType = interfaceType;
                    break;
                }
            }

            if (resolvableType == null)
            {
                currentType = currentType.BaseType;
            }
            else
            {
                if (resolvableType.IsGenericType)
                {
                    Type[] genArgs = resolvableType.GetGenericArguments();
                    bindableType = genArgs[position];
                }
                else
                {
                    bindableType = typeof(object);
                }
            }
        }

        if (bindableType == null)
        {
            throw new InvalidOperationException($"Cannot find parameter of {evaluatedClass.Name} for {interfaceClass} at position {position}");
        }

        return bindableType;
    }

    internal static bool CheckCompatiblePollableBinder(IBinder binderInstance, Type bindingTargetType)
    {
        Type binderInstanceType = binderInstance.GetType();
        Type[] binderInterfaces = binderInstanceType.GetInterfaces();

        foreach (Type binderInterface in binderInterfaces)
        {
            if (typeof(IPollableConsumerBinder).IsAssignableFrom(binderInterface))
            {
                Type[] targetInterfaces = bindingTargetType.GetInterfaces();
                Type psType = FindPollableSourceType(targetInterfaces);

                if (psType != null)
                {
                    return GetParameterType(binderInstance.GetType(), binderInterface, 0).IsAssignableFrom(psType);
                }
            }
        }

        return false;
    }

    internal static Type FindPollableSourceType(Type[] targetInterfaces)
    {
        foreach (Type targetInterface in targetInterfaces)
        {
            if (typeof(IPollableSource).IsAssignableFrom(targetInterface))
            {
                Type[] supers = targetInterface.GetInterfaces();

                foreach (Type type in supers)
                {
                    if (type.IsGenericType)
                    {
                        Type resolvableType = type.GetGenericTypeDefinition();

                        if (resolvableType.Equals(typeof(IPollableSource<>)))
                        {
                            return type.GetGenericArguments()[0];
                        }
                    }
                }
            }
        }

        return null;
    }
}
