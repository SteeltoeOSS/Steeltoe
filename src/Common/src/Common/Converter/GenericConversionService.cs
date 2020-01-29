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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Converter
{
    public class GenericConversionService : IConversionService, IConverterRegistry
    {
        private static readonly IGenericConverter NO_OP_CONVERTER = new NoOpConverter("NO_OP");
        private static readonly IGenericConverter NO_MATCH = new NoOpConverter("NO_MATCH");
        private readonly Converters _converters = new Converters();

        private readonly ConcurrentDictionary<ConverterCacheKey, IGenericConverter> _converterCache = new ConcurrentDictionary<ConverterCacheKey, IGenericConverter>();

        public bool CanConvert(Type sourceType, Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (sourceType == null)
            {
                return true;
            }

            var converter = GetConverter(sourceType, targetType);
            return converter != null;
        }

        public T Convert<T>(object source)
        {
            return (T)Convert(source, source.GetType(), typeof(T));
        }

        public object Convert(object source, Type sourceType, Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (sourceType == null)
            {
                if (source != null)
                {
                    throw new ArgumentException("Source must be [null] if source type == [null]");
                }

                return HandleResult(null, targetType, null);
            }

            if (source != null && !sourceType.IsInstanceOfType(source))
            {
                throw new ArgumentException("Source to convert from must be an instance of [" +
                        sourceType + "]; instead it was a [" + source.GetType().Name + "]");
            }

            var converter = GetConverter(sourceType, targetType);
            if (converter != null)
            {
                try
                {
                    var result = converter.Convert(source, sourceType, targetType);
                    return HandleResult(sourceType, targetType, result);
                }
                catch (ConversionFailedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ConversionFailedException(sourceType, targetType, source, ex);
                }
            }

            return HandleConverterNotFound(source, sourceType, targetType);
        }

        public bool CanBypassConvert(Type sourceType, Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (sourceType == null)
            {
                return true;
            }

            var converter = GetConverter(sourceType, targetType);
            return converter == NO_OP_CONVERTER;
        }

        public void AddConverter(IGenericConverter converter)
        {
            _converters.Add(converter);
            _converterCache.Clear();
        }

        protected virtual IGenericConverter GetConverter(Type sourceType, Type targetType)
        {
            var key = new ConverterCacheKey(sourceType, targetType);
            if (_converterCache.TryGetValue(key, out var converter))
            {
                return converter != NO_MATCH ? converter : null;
            }

            converter = _converters.Find(sourceType, targetType);
            if (converter == null)
            {
                converter = GetDefaultConverter(sourceType, targetType);
            }

            if (converter != null)
            {
                if (!_converterCache.TryAdd(key, converter))
                {
                    _converterCache.TryGetValue(key, out converter);
                }

                return converter;
            }

            _converterCache.TryAdd(key, NO_MATCH);
            return null;
        }

        protected virtual IGenericConverter GetDefaultConverter(Type sourceType, Type targetType)
        {
            return targetType.IsAssignableFrom(sourceType) ? NO_OP_CONVERTER : null;
        }

        private object HandleResult(Type sourceType, Type targetType, object result)
        {
            if (result == null)
            {
                AssertNotPrimitiveTargetType(sourceType, targetType);
            }

            return result;
        }

        private object HandleConverterNotFound(object source, Type sourceType, Type targetType)
        {
            if (source == null)
            {
                AssertNotPrimitiveTargetType(sourceType, targetType);
                return null;
            }

            if ((sourceType == null || targetType.IsAssignableFrom(sourceType)) && targetType.IsInstanceOfType(source))
            {
                return source;
            }

            throw new ConverterNotFoundException(sourceType, targetType);
        }

        private void AssertNotPrimitiveTargetType(Type sourceType, Type targetType)
        {
            if (targetType.IsPrimitive)
            {
                throw new ConversionFailedException(sourceType, targetType, null, new ArgumentException("A null value cannot be assigned to a primitive type"));
            }
        }

        private class NoOpConverter : IGenericConverter
        {
            private readonly string name;

            public NoOpConverter(string name)
            {
                this.name = name;
            }

            public ISet<(Type Source, Type Target)> ConvertibleTypes
            {
                get
                {
                    return null;
                }
            }

            public object Convert(object source, Type sourceType, Type targetType)
            {
                return source;
            }

            public override string ToString()
            {
                return name;
            }
        }

#pragma warning disable S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
        private sealed class ConverterCacheKey : IComparable<ConverterCacheKey>
#pragma warning restore S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
        {
            private readonly Type sourceType;

            private readonly Type targetType;

            public ConverterCacheKey(Type sourceType, Type targetType)
            {
                this.sourceType = sourceType;
                this.targetType = targetType;
            }

            public override bool Equals(object other)
            {
                if (this == other)
                {
                    return true;
                }

                if (!(other is ConverterCacheKey))
                {
                    return false;
                }

                var otherKey = (ConverterCacheKey)other;
                return sourceType.Equals(otherKey.sourceType) &&
                        targetType.Equals(otherKey.targetType);
            }

            public override int GetHashCode()
            {
                return (sourceType.GetHashCode() * 29) + targetType.GetHashCode();
            }

            public override string ToString()
            {
                return "ConverterCacheKey [sourceType = " + sourceType +
                        ", targetType = " + targetType + "]";
            }

            public int CompareTo(ConverterCacheKey other)
            {
                var result = sourceType.ToString().CompareTo(other.sourceType.ToString());
                if (result == 0)
                {
                    result = targetType.ToString().CompareTo(other.targetType.ToString());
                }

                return result;
            }
        }

        private class ConvertersForPair
        {
            private readonly LinkedList<IGenericConverter> converters = new LinkedList<IGenericConverter>();

            public void Add(IGenericConverter converter)
            {
                converters.AddFirst(converter);
            }

            public IGenericConverter GetConverter(Type sourceType, Type targetType)
            {
                foreach (var converter in converters)
                {
                    if (!(converter is IConditionalGenericConverter) ||
                            ((IConditionalGenericConverter)converter).Matches(sourceType, targetType))
                    {
                        return converter;
                    }
                }

                return null;
            }

            public override string ToString()
            {
                return string.Join(",", converters);
            }
        }

        private class Converters
        {
            private readonly ISet<IGenericConverter> globalConverters = new HashSet<IGenericConverter>();

            private readonly Dictionary<(Type Source, Type Target), ConvertersForPair> converters = new Dictionary<(Type Source, Type Target), ConvertersForPair>();

            public void Add(IGenericConverter converter)
            {
                var convertibleTypes = converter.ConvertibleTypes;
                if (convertibleTypes == null)
                {
                    if (!(converter is IConditionalConverter))
                    {
                        throw new InvalidOperationException("Only conditional converters may return null convertible types");
                    }

                    globalConverters.Add(converter);
                }
                else
                {
                    foreach (var convertiblePair in convertibleTypes)
                    {
                        var convertersForPair = GetMatchableConverters(convertiblePair);
                        convertersForPair.Add(converter);
                    }
                }
            }

            public IGenericConverter Find(Type sourceType, Type targetType)
            {
                // Search the full type hierarchy
                var sourceCandidates = GetClassHierarchy(sourceType);
                var targetCandidates = GetClassHierarchy(targetType);
                foreach (var sourceCandidate in sourceCandidates)
                {
                    var converter = CheckTargets(sourceType, targetType, sourceCandidate, targetCandidates);
                    if (converter != null)
                    {
                        return converter;
                    }

                    if (sourceCandidate.IsConstructedGenericType)
                    {
                        converter = CheckTargets(sourceType, targetType, sourceCandidate.GetGenericTypeDefinition(), targetCandidates);
                        if (converter != null)
                        {
                            return converter;
                        }
                    }
                }

                return null;
            }

            public IGenericConverter CheckTargets(Type sourceType, Type targetType, Type sourceCandidate, List<Type> targetCandidates)
            {
                foreach (var targetCandidate in targetCandidates)
                {
                    var convertiblePair = (Source: sourceCandidate, Target: targetCandidate);
                    var converter = GetRegisteredConverter(sourceType, targetType, convertiblePair);
                    if (converter != null)
                    {
                        return converter;
                    }

                    if (targetCandidate.IsConstructedGenericType)
                    {
                        convertiblePair.Target = targetCandidate.GetGenericTypeDefinition();
                        converter = GetRegisteredConverter(sourceType, targetType, convertiblePair);
                        if (converter != null)
                        {
                            return converter;
                        }
                    }
                }

                return null;
            }

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append("ConversionService converters =\n");
                foreach (var converterString in GetConverterStrings())
                {
                    builder.Append('\t').Append(converterString).Append('\n');
                }

                return builder.ToString();
            }

            private ConvertersForPair GetMatchableConverters((Type Source, Type Target) convertiblePair)
            {
                if (!converters.TryGetValue(convertiblePair, out var convertersForPair))
                {
                    convertersForPair = new ConvertersForPair();
                    converters.Add(convertiblePair, convertersForPair);
                }

                return convertersForPair;
            }

            private IGenericConverter GetRegisteredConverter(Type sourceType, Type targetType, (Type Source, Type Target) convertiblePair)
            {
                // Check specifically registered converters
                if (converters.TryGetValue(convertiblePair, out var convertersForPair))
                {
                    var converter = convertersForPair.GetConverter(sourceType, targetType);
                    if (converter != null)
                    {
                        return converter;
                    }
                }

                // Check ConditionalConverters for a dynamic match
                foreach (var globalConverter in globalConverters)
                {
                    if (((IConditionalConverter)globalConverter).Matches(sourceType, targetType))
                    {
                        return globalConverter;
                    }
                }

                return null;
            }

            private List<Type> GetClassHierarchy(Type type)
            {
                var hierarchy = new List<Type>();
                ISet<Type> visited = new HashSet<Type>();
                AddToClassHierarchy(0, type, false, hierarchy, visited);
                var array = type.IsArray;

                var i = 0;
                while (i < hierarchy.Count)
                {
                    var candidate = hierarchy[i];
                    candidate = array ? candidate.GetElementType() : candidate;
                    var superclass = candidate.BaseType;
                    if (superclass != null && superclass != typeof(object) && superclass != typeof(Enum))
                    {
                        AddToClassHierarchy(i + 1, candidate.BaseType, array, hierarchy, visited);
                    }

                    AddInterfacesToClassHierarchy(candidate, array, hierarchy, visited);
                    i++;
                }

                if (typeof(Enum).IsAssignableFrom(type))
                {
                    AddToClassHierarchy(hierarchy.Count, typeof(Enum), array, hierarchy, visited);
                    AddToClassHierarchy(hierarchy.Count, typeof(Enum), false, hierarchy, visited);
                    AddInterfacesToClassHierarchy(typeof(Enum), array, hierarchy, visited);
                }

                AddToClassHierarchy(hierarchy.Count, typeof(object), array, hierarchy, visited);
                AddToClassHierarchy(hierarchy.Count, typeof(object), false, hierarchy, visited);
                return hierarchy;
            }

            private void AddInterfacesToClassHierarchy(Type type, bool asArray, List<Type> hierarchy, ISet<Type> visited)
            {
                foreach (var implementedInterface in type.GetInterfaces())
                {
                    AddToClassHierarchy(hierarchy.Count, implementedInterface, asArray, hierarchy, visited);
                }
            }

            private void AddToClassHierarchy(int index, Type type, bool asArray, List<Type> hierarchy, ISet<Type> visited)
            {
                if (asArray)
                {
                    type = Array.CreateInstance(type, 0).GetType();
                }

                if (visited.Add(type))
                {
                    hierarchy.Insert(index, type);
                }
            }

            private List<string> GetConverterStrings()
            {
                var converterStrings = new List<string>();
                foreach (var convertersForPair in converters.Values)
                {
                    converterStrings.Add(convertersForPair.ToString());
                }

                converterStrings.Sort();
                return converterStrings;
            }
        }
    }
}
