// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Support
{
    public class SimpleEvaluationContext : IEvaluationContext
    {
        private static readonly List<IConstructorResolver> _emptyConstrResolver = new ();
        private readonly ITypeLocator _typeNotFoundLocator = new TypeNotFoundTypeLocator();
        private readonly ITypedValue _rootObject;
        private readonly List<IPropertyAccessor> _propertyAccessors;
        private readonly List<IMethodResolver> _methodResolvers;
        private readonly ITypeConverter _typeConverter;
        private readonly ITypeComparator _typeComparator = new StandardTypeComparator();
        private readonly IOperatorOverloader _operatorOverloader = new StandardOperatorOverloader();
        private readonly Dictionary<string, object> _variables = new ();

        private SimpleEvaluationContext(List<IPropertyAccessor> accessors, List<IMethodResolver> resolvers, ITypeConverter converter, ITypedValue rootObject)
        {
            _propertyAccessors = accessors;
            _methodResolvers = resolvers;
            _typeConverter = converter ?? new StandardTypeConverter();
            _rootObject = rootObject ?? TypedValue.NULL;
        }

        public ITypedValue RootObject => _rootObject;

        public List<IPropertyAccessor> PropertyAccessors => _propertyAccessors;

        public List<IConstructorResolver> ConstructorResolvers => _emptyConstrResolver;

        public List<IMethodResolver> MethodResolvers => _methodResolvers;

        public IServiceResolver ServiceResolver => null;

        public ITypeLocator TypeLocator => _typeNotFoundLocator;

        public ITypeConverter TypeConverter => _typeConverter;

        public ITypeComparator TypeComparator => _typeComparator;

        public IOperatorOverloader OperatorOverloader => _operatorOverloader;

        public static Builder ForPropertyAccessors(params IPropertyAccessor[] accessors)
        {
            foreach (var accessor in accessors)
            {
                if (accessor.GetType() == typeof(ReflectivePropertyAccessor))
                {
                    throw new InvalidOperationException("SimpleEvaluationContext is not designed for use with a plain " + "ReflectivePropertyAccessor. Consider using DataBindingPropertyAccessor or a custom subclass.");
                }
            }

            return new Builder(accessors);
        }

        public static Builder ForReadOnlyDataBinding()
        {
            return new Builder(DataBindingPropertyAccessor.ForReadOnlyAccess());
        }

        public static Builder ForReadWriteDataBinding()
        {
            return new Builder(DataBindingPropertyAccessor.ForReadWriteAccess());
        }

        public void SetVariable(string name, object value)
        {
            _variables[name] = value;
        }

        public object LookupVariable(string name)
        {
            _variables.TryGetValue(name, out var result);
            return result;
        }

        public T LookupVariable<T>(string name)
        {
            _variables.TryGetValue(name, out var result);
            return (T)result;
        }

        public class Builder
        {
            private readonly List<IPropertyAccessor> _accessors;
            private List<IMethodResolver> _resolvers = new ();
            private ITypeConverter _typeConverter;
            private ITypedValue _rootObject;

            public Builder(params IPropertyAccessor[] accessors)
            {
                _accessors = new List<IPropertyAccessor>(accessors);
            }

            public Builder WithMethodResolvers(params IMethodResolver[] resolvers)
            {
                foreach (var resolver in resolvers)
                {
                    if (resolver.GetType() == typeof(ReflectiveMethodResolver))
                    {
                        throw new InvalidOperationException("SimpleEvaluationContext is not designed for use with a plain " + "ReflectiveMethodResolver. Consider using DataBindingMethodResolver or a custom subclass.");
                    }
                }

                _resolvers = new List<IMethodResolver>(resolvers);
                return this;
            }

            public Builder WithInstanceMethods()
            {
                _resolvers = new List<IMethodResolver>() { DataBindingMethodResolver.ForInstanceMethodInvocation() };
                return this;
            }

            public Builder WithConversionService(IConversionService conversionService)
            {
                _typeConverter = new StandardTypeConverter(conversionService);
                return this;
            }

            public Builder WithTypeConverter(ITypeConverter converter)
            {
                _typeConverter = converter;
                return this;
            }

            public Builder WithRootObject(object rootObject)
            {
                _rootObject = new TypedValue(rootObject);
                return this;
            }

            public Builder WithTypedRootObject(object rootObject, Type typeDescriptor)
            {
                _rootObject = new TypedValue(rootObject, typeDescriptor);
                return this;
            }

            public SimpleEvaluationContext Build()
            {
                return new SimpleEvaluationContext(_accessors, _resolvers, _typeConverter, _rootObject);
            }
        }

        private class TypeNotFoundTypeLocator : ITypeLocator
        {
            public Type FindType(string typeName)
            {
                throw new SpelEvaluationException(SpelMessage.TYPE_NOT_FOUND, typeName);
            }
        }
    }
}
