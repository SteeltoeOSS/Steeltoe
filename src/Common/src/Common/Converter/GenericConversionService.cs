// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Text;

namespace Steeltoe.Common.Converter;

public class GenericConversionService : IConversionService, IConverterRegistry
{
    private static readonly IGenericConverter NoOpConverterSingleton = new NoOpConverter("NO_OP");
    private static readonly IGenericConverter NoMatchSingleton = new NoOpConverter("NO_MATCH");
    private readonly Converters _converters = new();

    private readonly ConcurrentDictionary<ConverterCacheKey, IGenericConverter> _converterCache = new();

    public bool CanConvert(Type sourceType, Type targetType)
    {
        ArgumentGuard.NotNull(targetType);

        if (sourceType == null)
        {
            return true;
        }

        IGenericConverter converter = GetConverter(sourceType, targetType);
        return converter != null;
    }

    public T Convert<T>(object source)
    {
        return (T)Convert(source, source.GetType(), typeof(T));
    }

    public object Convert(object source, Type sourceType, Type targetType)
    {
        ArgumentGuard.NotNull(targetType);

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
            throw new ArgumentException($"Source to convert from must be an instance of [{sourceType.Name}]; instead it was a [{source.GetType().Name}]");
        }

        IGenericConverter converter = GetConverter(sourceType, targetType);

        if (converter != null)
        {
            try
            {
                object result = converter.Convert(source, sourceType, targetType);
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
        ArgumentGuard.NotNull(targetType);

        if (sourceType == null)
        {
            return true;
        }

        IGenericConverter converter = GetConverter(sourceType, targetType);
        return converter == NoOpConverterSingleton;
    }

    public void AddConverter(IGenericConverter converter)
    {
        _converters.Add(converter);
        _converterCache.Clear();
    }

    protected virtual IGenericConverter GetConverter(Type sourceType, Type targetType)
    {
        var key = new ConverterCacheKey(sourceType, targetType);

        if (_converterCache.TryGetValue(key, out IGenericConverter converter))
        {
            return converter != NoMatchSingleton ? converter : null;
        }

        converter = _converters.Find(sourceType, targetType) ?? GetDefaultConverter(sourceType, targetType);

        if (converter != null)
        {
            if (!_converterCache.TryAdd(key, converter))
            {
                _converterCache.TryGetValue(key, out converter);
            }

            return converter;
        }

        _converterCache.TryAdd(key, NoMatchSingleton);
        return null;
    }

    protected virtual IGenericConverter GetDefaultConverter(Type sourceType, Type targetType)
    {
        return targetType.IsAssignableFrom(sourceType) ? NoOpConverterSingleton : null;
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
            throw new ConversionFailedException(sourceType, targetType, null, new ArgumentException("A null value cannot be assigned to a primitive type."));
        }
    }

    private sealed class NoOpConverter : IGenericConverter
    {
        private readonly string _name;

        public ISet<(Type Source, Type Target)> ConvertibleTypes => null;

        public NoOpConverter(string name)
        {
            _name = name;
        }

        public object Convert(object source, Type sourceType, Type targetType)
        {
            return source;
        }

        public override string ToString()
        {
            return _name;
        }
    }

#pragma warning disable S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
    private sealed class ConverterCacheKey : IComparable<ConverterCacheKey>
#pragma warning restore S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
    {
        private readonly Type _sourceType;

        private readonly Type _targetType;

        public ConverterCacheKey(Type sourceType, Type targetType)
        {
            _sourceType = sourceType;
            _targetType = targetType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not ConverterCacheKey other)
            {
                return false;
            }

            return _sourceType == other._sourceType && _targetType == other._targetType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_sourceType, _targetType);
        }

        public override string ToString()
        {
            return $"ConverterCacheKey [sourceType = {_sourceType}, targetType = {_targetType}]";
        }

        public int CompareTo(ConverterCacheKey other)
        {
            int result = _sourceType.ToString().CompareTo(other._sourceType.ToString());

            if (result == 0)
            {
                result = _targetType.ToString().CompareTo(other._targetType.ToString());
            }

            return result;
        }
    }

    private sealed class ConvertersForPair
    {
        private readonly LinkedList<IGenericConverter> _converters = new();

        public void Add(IGenericConverter converter)
        {
            _converters.AddFirst(converter);
        }

        public IGenericConverter GetConverter(Type sourceType, Type targetType)
        {
            foreach (IGenericConverter converter in _converters)
            {
                if (converter is not IConditionalGenericConverter genericConverter || genericConverter.Matches(sourceType, targetType))
                {
                    return converter;
                }
            }

            return null;
        }

        public override string ToString()
        {
            return string.Join(",", _converters);
        }
    }

    private sealed class Converters
    {
        private readonly ISet<IGenericConverter> _globalConverters = new HashSet<IGenericConverter>();

        private readonly Dictionary<(Type Source, Type Target), ConvertersForPair> _converters = new();

        public void Add(IGenericConverter converter)
        {
            ISet<(Type Source, Type Target)> convertibleTypes = converter.ConvertibleTypes;

            if (convertibleTypes == null)
            {
                if (converter is not IConditionalConverter)
                {
                    throw new InvalidOperationException("Only conditional converters may return null convertible types");
                }

                _globalConverters.Add(converter);
            }
            else
            {
                foreach ((Type Source, Type Target) convertiblePair in convertibleTypes)
                {
                    ConvertersForPair convertersForPair = GetMatchableConverters(convertiblePair);
                    convertersForPair.Add(converter);
                }
            }
        }

        public IGenericConverter Find(Type sourceType, Type targetType)
        {
            // Search the full type hierarchy
            List<Type> sourceCandidates = GetClassHierarchy(sourceType);
            List<Type> targetCandidates = GetClassHierarchy(targetType);

            foreach (Type sourceCandidate in sourceCandidates)
            {
                IGenericConverter converter = CheckTargets(sourceType, targetType, sourceCandidate, targetCandidates);

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
            foreach (Type targetCandidate in targetCandidates)
            {
                (Type Source, Type Target) convertiblePair = (Source: sourceCandidate, Target: targetCandidate);
                IGenericConverter converter = GetRegisteredConverter(sourceType, targetType, convertiblePair);

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

            foreach (string converterString in GetConverterStrings())
            {
                builder.Append('\t').Append(converterString).Append('\n');
            }

            return builder.ToString();
        }

        private ConvertersForPair GetMatchableConverters((Type Source, Type Target) convertiblePair)
        {
            if (!_converters.TryGetValue(convertiblePair, out ConvertersForPair convertersForPair))
            {
                convertersForPair = new ConvertersForPair();
                _converters.Add(convertiblePair, convertersForPair);
            }

            return convertersForPair;
        }

        private IGenericConverter GetRegisteredConverter(Type sourceType, Type targetType, (Type Source, Type Target) convertiblePair)
        {
            // Check specifically registered converters
            if (_converters.TryGetValue(convertiblePair, out ConvertersForPair convertersForPair))
            {
                IGenericConverter converter = convertersForPair.GetConverter(sourceType, targetType);

                if (converter != null)
                {
                    return converter;
                }
            }

            // Check ConditionalConverters for a dynamic match
            foreach (IGenericConverter globalConverter in _globalConverters)
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
            bool array = type.IsArray;

            int i = 0;

            while (i < hierarchy.Count)
            {
                Type candidate = hierarchy[i];
                candidate = array ? candidate.GetElementType() : candidate;
                Type superclass = candidate.BaseType;

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
            foreach (Type implementedInterface in type.GetInterfaces())
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

            foreach (ConvertersForPair convertersForPair in _converters.Values)
            {
                converterStrings.Add(convertersForPair.ToString());
            }

            converterStrings.Sort();
            return converterStrings;
        }
    }
}
