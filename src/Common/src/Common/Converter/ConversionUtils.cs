// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Common.Converter
{
    public static class ConversionUtils
    {
        private const string DELIMITER = ",";

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
            foreach (var sourceElement in collection)
            {
                var targetElement = conversionService.Convert(sourceElement, sourceElement.GetType(), targetType);
                sj.Append(targetElement.ToString());
                sj.Append(DELIMITER);
            }

            return sj.ToString(0, sj.Length - 1);
        }

        public static int Count(IEnumerable enumerable)
        {
            if (enumerable is ICollection)
            {
                return ((ICollection)enumerable).Count;
            }

            var count = 0;
            foreach (var element in enumerable)
            {
                count++;
            }

            return count;
        }

        public static Type GetNullableElementType(Type nullable)
        {
            if (nullable.IsGenericType && typeof(System.Nullable<>) == nullable.GetGenericTypeDefinition())
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
                    var defn = type.GetGenericTypeDefinition();
                    if (typeof(IList<>) == defn)
                    {
                        return true;
                    }

                    if (typeof(IEnumerator<>) == defn)
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
                    var defn = type.GetGenericTypeDefinition();
                    if (typeof(IDictionary<,>) == defn)
                    {
                        return true;
                    }

                    if (typeof(IEnumerator<>) == defn)
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

            return collectionType.GetConstructor(Array.Empty<Type>()) != null;
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
                    var defn = type.GetGenericTypeDefinition();
                    if (typeof(IList<>) == defn)
                    {
                        return (IList)Activator.CreateInstance(MakeGenericListType(type));
                    }

                    if (typeof(IEnumerable<>) == defn)
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
            else
            {
                return (IList)Activator.CreateInstance(type);
            }
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
                    var defn = type.GetGenericTypeDefinition();
                    if (typeof(IDictionary<,>) == defn)
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
            else
            {
                return (IDictionary)Activator.CreateInstance(type);
            }
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
                var method = clazz.GetMethod(methodName, args);
                return method.IsStatic ? method : null;
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
            else
            {
                var candidates = FindMethodCandidatesByName(clazz, methodName);
                if (candidates.Count == 1)
                {
                    return candidates[0];
                }

                return null;
            }
        }

        internal static List<MethodInfo> FindMethodCandidatesByName(Type clazz, string methodName)
        {
            var candidates = new List<MethodInfo>();
            var methods = clazz.GetMethods();
            foreach (var method in methods)
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
            var elemType = type.GetGenericArguments()[0];
            return typeof(List<>).MakeGenericType(new Type[] { elemType });
        }

        internal static Type MakeGenericDictionaryType(Type type)
        {
            var keyType = type.GetGenericArguments()[0];
            var valType = type.GetGenericArguments()[0];
            return typeof(Dictionary<,>).MakeGenericType(new Type[] { keyType, valType });
        }
    }
}
