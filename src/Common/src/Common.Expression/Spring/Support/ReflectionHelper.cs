// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Reflection;
using Steeltoe.Common.Util;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public static class ReflectionHelper
{
    public static ArgumentsMatchInfo CompareArguments(IReadOnlyList<Type> expectedArgTypes, IReadOnlyList<Type> suppliedArgTypes, ITypeConverter typeConverter)
    {
        if (expectedArgTypes.Count != suppliedArgTypes.Count)
        {
            throw new InvalidOperationException("Expected argument types and supplied argument types should be arrays of same length");
        }

        ArgumentsMatchKind? match = ArgumentsMatchKind.Exact;

        for (int i = 0; i < expectedArgTypes.Count && match != null; i++)
        {
            Type suppliedArg = suppliedArgTypes[i];
            Type expectedArg = expectedArgTypes[i];

            // The user may supply null - and that will be ok unless a primitive is expected
            if (suppliedArg == null)
            {
                if (expectedArg.IsPrimitive)
                {
                    match = null;
                }
            }
            else if (!expectedArg.Equals(suppliedArg))
            {
                // if (suppliedArg.IsAssignableTo(expectedArg))
                if (expectedArg.IsAssignableFrom(suppliedArg))
                {
                    if (match != ArgumentsMatchKind.RequiresConversion)
                    {
                        match = ArgumentsMatchKind.Close;
                    }
                }
                else if (typeConverter.CanConvert(suppliedArg, expectedArg))
                {
                    match = ArgumentsMatchKind.RequiresConversion;
                }
                else
                {
                    match = null;
                }
            }
        }

        return match != null ? new ArgumentsMatchInfo(match.Value) : null;
    }

    public static int GetTypeDifferenceWeight(IReadOnlyList<Type> paramTypes, IReadOnlyList<Type> argTypes)
    {
        int result = 0;

        for (int i = 0; i < paramTypes.Count; i++)
        {
            Type paramType = paramTypes[i];
            Type argType = i < argTypes.Count ? argTypes[i] : null;

            if (argType == null)
            {
                if (paramType.IsPrimitive)
                {
                    return int.MaxValue;
                }
            }
            else
            {
                Type currentParamType = paramType;

                if (!ClassUtils.IsAssignable(currentParamType, argType))
                {
                    return int.MaxValue;
                }

                if (currentParamType.IsPrimitive)
                {
                    currentParamType = typeof(object);
                }

                Type superClass = argType.BaseType;

                while (superClass != null)
                {
                    if (currentParamType.Equals(superClass))
                    {
                        result += 2;
                        superClass = null;
                    }
                    else if (ClassUtils.IsAssignable(currentParamType, superClass))
                    {
                        result += 2;
                        superClass = superClass.BaseType;
                    }
                    else
                    {
                        superClass = null;
                    }
                }

                if (currentParamType.IsInterface)
                {
                    result++;
                }
            }
        }

        return result;
    }

    public static ArgumentsMatchInfo CompareArgumentsVarargs(IReadOnlyList<Type> expectedArgTypes, IReadOnlyList<Type> suppliedArgTypes,
        ITypeConverter typeConverter)
    {
        if (expectedArgTypes == null || expectedArgTypes.Count == 0)
        {
            throw new InvalidOperationException("Expected arguments must at least include one array (the varargs parameter)");
        }

        if (!expectedArgTypes[expectedArgTypes.Count - 1].IsArray)
        {
            throw new InvalidOperationException("Final expected argument should be array type (the varargs parameter)");
        }

        ArgumentsMatchKind? match = ArgumentsMatchKind.Exact;

        // Check up until the varargs argument:

        // Deal with the arguments up to 'expected number' - 1 (that is everything but the varargs argument)
        int argCountUpToVarargs = expectedArgTypes.Count - 1;

        for (int i = 0; i < argCountUpToVarargs && match != null; i++)
        {
            Type suppliedArg = suppliedArgTypes[i];
            Type expectedArg = expectedArgTypes[i];

            if (suppliedArg == null)
            {
                if (expectedArg.IsPrimitive)
                {
                    match = null;
                }
            }
            else
            {
                if (!expectedArg.Equals(suppliedArg))
                {
                    if (expectedArg.IsAssignableFrom(suppliedArg))
                    {
                        if (match != ArgumentsMatchKind.RequiresConversion)
                        {
                            match = ArgumentsMatchKind.Close;
                        }
                    }
                    else if (typeConverter.CanConvert(suppliedArg, expectedArg))
                    {
                        match = ArgumentsMatchKind.RequiresConversion;
                    }
                    else
                    {
                        match = null;
                    }
                }
            }
        }

        // If already confirmed it cannot be a match, then return
        if (match == null)
        {
            return null;
        }

        if (suppliedArgTypes.Count == expectedArgTypes.Count &&
            expectedArgTypes[expectedArgTypes.Count - 1].Equals(suppliedArgTypes[suppliedArgTypes.Count - 1]))
        {
            // Special case: there is one parameter left and it is an array and it matches the varargs
            // expected argument - that is a match, the caller has already built the array. Proceed with it.
        }
        else
        {
            // Now... we have the final argument in the method we are checking as a match and we have 0
            // or more other arguments left to pass to it.
            Type varargsDesc = expectedArgTypes[expectedArgTypes.Count - 1];

            if (!varargsDesc.HasElementType)
            {
                throw new InvalidOperationException("No element type");
            }

            Type varargsParamType = varargsDesc.GetElementType();

            // All remaining parameters must be of this type or convertible to this type
            for (int i = expectedArgTypes.Count - 1; i < suppliedArgTypes.Count; i++)
            {
                Type suppliedArg = suppliedArgTypes[i];

                if (suppliedArg == null)
                {
                    if (varargsParamType.IsPrimitive)
                    {
                        match = null;
                    }
                }
                else
                {
                    if (varargsParamType != suppliedArg)
                    {
                        if (ClassUtils.IsAssignable(varargsParamType, suppliedArg))
                        {
                            if (match != ArgumentsMatchKind.RequiresConversion)
                            {
                                match = ArgumentsMatchKind.Close;
                            }
                        }
                        else if (typeConverter.CanConvert(suppliedArg, varargsParamType))
                        {
                            match = ArgumentsMatchKind.RequiresConversion;
                        }
                        else
                        {
                            match = null;
                        }
                    }
                }
            }
        }

        return match != null ? new ArgumentsMatchInfo(match.Value) : null;
    }

    public static ConstructorInfo GetAccessibleConstructor(Type type, params Type[] paramTypes)
    {
        paramTypes ??= Type.EmptyTypes;

        return type.GetConstructor(paramTypes);
    }

    public static bool ConvertAllArguments(ITypeConverter converter, object[] arguments, MethodInfo method)
    {
        int? varargsPosition = method.IsVarArgs() ? method.GetParameters().Length - 1 : null;
        return ConvertArguments(converter, arguments, method, varargsPosition);
    }

    public static object[] SetupArgumentsForVarargsInvocation(Type[] requiredParameterTypes, params object[] args)
    {
        // Check if array already built for final argument
        int parameterCount = requiredParameterTypes.Length;
        int argumentCount = args.Length;

        // Check if repackaging is needed...
        if (parameterCount != args.Length || requiredParameterTypes[parameterCount - 1] != args[argumentCount - 1]?.GetType())
        {
            int arraySize = 0; // zero size array if nothing to pass as the varargs parameter

            if (argumentCount >= parameterCount)
            {
                arraySize = argumentCount - (parameterCount - 1);
            }

            // Create an array for the varargs arguments
            object[] newArgs = new object[parameterCount];
            Array.Copy(args, 0, newArgs, 0, newArgs.Length - 1);

            // Now sort out the final argument, which is the varargs one. Before entering this method,
            // the arguments should have been converted to the box form of the required type.
            Type componentType = requiredParameterTypes[parameterCount - 1].GetElementType();
            var repackagedArgs = Array.CreateInstance(componentType, arraySize);

            for (int i = 0; i < arraySize; i++)
            {
                repackagedArgs.SetValue(args[parameterCount - 1 + i], i);
            }

            newArgs[newArgs.Length - 1] = repackagedArgs;
            return newArgs;
        }

        return args;
    }

    public static bool ConvertArguments(ITypeConverter converter, object[] arguments, MethodBase executable, int? varargsPosition)
    {
        bool conversionOccurred = false;

        if (varargsPosition == null)
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                Type targetType = executable.GetParameters()[i].ParameterType;
                object argument = arguments[i];
                arguments[i] = converter.ConvertValue(argument, argument == null ? typeof(object) : argument.GetType(), targetType);
                conversionOccurred |= argument != arguments[i];
            }
        }
        else
        {
            // Convert everything up to the varargs position
            for (int i = 0; i < varargsPosition; i++)
            {
                Type targetType = executable.GetParameters()[i].ParameterType;
                object argument = arguments[i];
                arguments[i] = converter.ConvertValue(argument, argument == null ? typeof(object) : argument.GetType(), targetType);
                conversionOccurred |= argument != arguments[i];
            }

            int vPos = varargsPosition.Value;
            ParameterInfo methodParam = executable.GetParameters()[vPos];

            if (vPos == arguments.Length - 1)
            {
                // If the target is varargs and there is just one more argument
                // then convert it here
                Type targetType = methodParam.ParameterType;
                object argument = arguments[vPos];
                Type sourceType = argument == null ? typeof(object) : argument.GetType();
                arguments[vPos] = converter.ConvertValue(argument, sourceType, targetType);

                // Three outcomes of that previous line:
                // 1) the input argument was already compatible (ie. array of valid type) and nothing was done
                // 2) the input argument was correct type but not in an array so it was made into an array
                // 3) the input argument was the wrong type and got converted and put into an array
                if (argument != arguments[vPos] && !IsFirstEntryInArray(argument, arguments[vPos]))
                {
                    conversionOccurred = true; // case 3
                }
            }
            else
            {
                // Convert remaining arguments to the varargs element type
                Type targetType = methodParam.ParameterType.GetElementType();

                if (targetType == null)
                {
                    throw new InvalidOperationException("No element type");
                }

                for (int i = vPos; i < arguments.Length; i++)
                {
                    object argument = arguments[i];
                    arguments[i] = converter.ConvertValue(argument, argument == null ? typeof(object) : argument.GetType(), targetType);
                    conversionOccurred |= argument != arguments[i];
                }
            }
        }

        return conversionOccurred;
    }

    public static Type GetMapValueTypeDescriptor(Type targetDescriptor)
    {
        if (!typeof(IDictionary).IsAssignableFrom(targetDescriptor))
        {
            throw new InvalidOperationException("Not a IDictionary");
        }

        if (!targetDescriptor.IsGenericType)
        {
            return null;
        }

        return targetDescriptor.GetGenericArguments()[1];
    }

    public static Type GetMapValueTypeDescriptor(Type targetDescriptor, object mapValue)
    {
        Type type = GetMapValueTypeDescriptor(targetDescriptor);

        if (type != null)
        {
            if (mapValue == null)
            {
                return type;
            }

            return mapValue.GetType();
        }

        if (mapValue != null)
        {
            return mapValue.GetType();
        }

        return null;
    }

    public static Type GetMapKeyTypeDescriptor(Type targetDescriptor)
    {
        if (!typeof(IDictionary).IsAssignableFrom(targetDescriptor))
        {
            throw new InvalidOperationException("Not a IDictionary");
        }

        if (!targetDescriptor.IsGenericType)
        {
            return null;
        }

        return targetDescriptor.GetGenericArguments()[0];
    }

    public static Type GetMapKeyTypeDescriptor(Type targetDescriptor, object mapKey)
    {
        Type type = GetMapKeyTypeDescriptor(targetDescriptor);

        if (type != null)
        {
            if (mapKey == null)
            {
                return type;
            }

            return mapKey.GetType();
        }

        if (mapKey != null)
        {
            return mapKey.GetType();
        }

        return null;
    }

    public static Type GetElementTypeDescriptor(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            return type.GetGenericArguments()[0];
        }

        return null;
    }

    public static Type GetElementTypeDescriptor(Type type, object obj)
    {
        Type elemType = GetElementTypeDescriptor(type);

        if (elemType != null)
        {
            if (obj == null)
            {
                return type;
            }

            return obj.GetType();
        }

        if (obj != null)
        {
            return obj.GetType();
        }

        return null;
    }

    public static bool IsPublic(Type type)
    {
        if (type.IsNested)
        {
            return type.DeclaringType.IsPublic && type.IsNestedPublic;
        }

        return type.IsPublic;
    }

    private static bool IsFirstEntryInArray(object value, object possibleArray)
    {
        if (possibleArray == null)
        {
            return false;
        }

        Type type = possibleArray.GetType();

        if (!type.IsArray || ((Array)possibleArray).GetLength(0) == 0 || !ClassUtils.IsAssignableValue(type.GetElementType(), value))
        {
            return false;
        }

        object arrayValue = ((Array)possibleArray).GetValue(0);
        return type.GetElementType().IsPrimitive ? arrayValue.Equals(value) : arrayValue == value;
    }
}
