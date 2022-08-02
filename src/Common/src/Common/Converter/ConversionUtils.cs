// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Reflection;
using System.Text;

namespace Steeltoe.Common.Converter;

public static class ConversionUtils
{
    private const string Delimiter = ",";

    public static bool CanConvertElements(Type sourceElementType, Type targetElementType, IConversionService conversionService)
    {
        if (targetElementType == null)
        {
            // yes
            return true;
        }

        if (sourceElementType == null)
        {
            // maybe
            return true;
        }

        if (conversionService.CanConvert(sourceElementType, targetElementType))
        {
            // yes
            return true;
        }

        if (sourceElementType.IsAssignableFrom(targetElementType))
        {
            // maybe
            return true;
        }

        // no
        return false;
    }

    public static Type GetElementType(Type sourceType)
    {
        if (sourceType.HasElementType)
        {
            return sourceType.GetElementType();
        }

        if (sourceType.IsConstructedGenericType)
        {
            return sourceType.GetGenericArguments()[0];
        }

        return null;
    }

    public static string ToString(IEnumerable collection, Type targetType, IConversionService conversionService)
    {
        var sj = new StringBuilder();

        foreach (object sourceElement in collection)
        {
            object targetElement = conversionService.Convert(sourceElement, sourceElement.GetType(), targetType);
            sj.Append(targetElement);
            sj.Append(Delimiter);
        }

        return sj.ToString(0, sj.Length - 1);
    }

    public static int Count(IEnumerable enumerable)
    {
        if (enumerable is ICollection collection)
        {
            return collection.Count;
        }

        return enumerable.Cast<object>().Count();
    }

    public static Type GetNullableElementType(Type nullable)
    {
        if (nullable.IsGenericType && typeof(Nullable<>) == nullable.GetGenericTypeDefinition())
        {
            return nullable.GetGenericArguments()[0];
        }

        return nullable;
    }

    public static bool CanCreateCompatListFor(Type type)
    {
        if (type == null)
        {
            return false;
        }

        if (type.IsGenericTypeDefinition)
        {
            return false;
        }

        if (type.IsInterface)
        {
            if (typeof(IList).IsAssignableFrom(type))
            {
                return true;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return true;
            }

            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();

                if (typeof(IList<>) == definition || typeof(IEnumerator<>) == definition || typeof(ICollection<>) == definition)
                {
                    return true;
                }
            }
        }
        else
        {
            return typeof(IList).IsAssignableFrom(type) && ContainsPublicNoArgConstructor(type);
        }

        return false;
    }

    public static bool CanCreateCompatDictionaryFor(Type type)
    {
        if (type == null)
        {
            return false;
        }

        if (type.IsGenericTypeDefinition)
        {
            return false;
        }

        if (type.IsInterface)
        {
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return true;
            }

            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();

                if (typeof(IDictionary<,>) == definition)
                {
                    return true;
                }

                if (typeof(IEnumerator<>) == definition)
                {
                    return true;
                }
            }
        }
        else
        {
            return typeof(IDictionary).IsAssignableFrom(type) && ContainsPublicNoArgConstructor(type);
        }

        return false;
    }

    public static Type GetDictionaryKeyType(Type sourceType)
    {
        return GetDictionaryKeyOrValueType(sourceType, 0);
    }

    public static Type GetDictionaryValueType(Type sourceType)
    {
        return GetDictionaryKeyOrValueType(sourceType, 1);
    }

    public static bool ContainsPublicNoArgConstructor(Type collectionType)
    {
        if (collectionType == null)
        {
            return false;
        }

        return collectionType.GetConstructor(Type.EmptyTypes) != null;
    }

    public static IList CreateCompatListFor(Type type)
    {
        if (!CanCreateCompatListFor(type))
        {
            return null;
        }

        if (type.IsInterface)
        {
            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();

                if (typeof(IList<>) == definition || typeof(IEnumerable<>) == definition || typeof(ICollection<>) == definition)
                {
                    return (IList)Activator.CreateInstance(MakeGenericListType(type));
                }
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                return new ArrayList();
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return new ArrayList();
            }

            return null;
        }

        return (IList)Activator.CreateInstance(type);
    }

    public static IDictionary CreateCompatDictionaryFor(Type type)
    {
        if (!CanCreateCompatDictionaryFor(type))
        {
            return null;
        }

        if (type.IsInterface)
        {
            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();

                if (typeof(IDictionary<,>) == definition)
                {
                    return (IDictionary)Activator.CreateInstance(MakeGenericDictionaryType(type));
                }
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return new Hashtable();
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return new Hashtable();
            }

            return null;
        }

        return (IDictionary)Activator.CreateInstance(type);
    }

    public static ConstructorInfo GetConstructorIfAvailable(Type clazz, params Type[] paramTypes)
    {
        if (clazz == null)
        {
            throw new ArgumentNullException(nameof(clazz));
        }

        try
        {
            return clazz.GetConstructor(paramTypes);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static MethodInfo GetStaticMethod(Type clazz, string methodName, params Type[] args)
    {
        if (clazz == null)
        {
            throw new ArgumentNullException(nameof(clazz));
        }

        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException(nameof(methodName));
        }

        try
        {
            MethodInfo method = clazz.GetMethod(methodName, args);

            if (method != null)
            {
                return method.IsStatic ? method : null;
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static MethodInfo GetMethodIfAvailable(Type clazz, string methodName, params Type[] paramTypes)
    {
        if (clazz == null)
        {
            throw new ArgumentNullException(nameof(clazz));
        }

        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException(nameof(methodName));
        }

        if (paramTypes != null)
        {
            try
            {
                return clazz.GetMethod(methodName, paramTypes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        List<MethodInfo> candidates = FindMethodCandidatesByName(clazz, methodName);

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        return null;
    }

    internal static List<MethodInfo> FindMethodCandidatesByName(Type clazz, string methodName)
    {
        var candidates = new List<MethodInfo>();
        MethodInfo[] methods = clazz.GetMethods();

        foreach (MethodInfo method in methods)
        {
            if (methodName.Equals(method.Name))
            {
                candidates.Add(method);
            }
        }

        return candidates;
    }

    internal static Type GetDictionaryKeyOrValueType(Type sourceType, int index)
    {
        if (sourceType.IsGenericType)
        {
            return sourceType.GetGenericArguments()[index];
        }

        return typeof(object);
    }

    internal static Type MakeGenericListType(Type type)
    {
        Type elemType = type.GetGenericArguments()[0];
        return typeof(List<>).MakeGenericType(elemType);
    }

    internal static Type MakeGenericDictionaryType(Type type)
    {
        Type keyType = type.GetGenericArguments()[0];
        Type valType = type.GetGenericArguments()[1];
        return typeof(Dictionary<,>).MakeGenericType(keyType, valType);
    }
}
