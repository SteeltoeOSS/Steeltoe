// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class StandardEvaluationContext : IEvaluationContext
{
    private readonly ConcurrentDictionary<string, object> _variables = new ();

    private volatile List<IPropertyAccessor> _propertyAccessors;

    private volatile List<IConstructorResolver> _constructorResolvers;

    private volatile List<IMethodResolver> _methodResolvers;

    private volatile ReflectiveMethodResolver _reflectiveMethodResolver;

    private ITypeLocator _typeLocator;

    private ITypeConverter _typeConverter;

    private ITypeComparator _typeComparator = new StandardTypeComparator();

    private IOperatorOverloader _operatorOverloader = new StandardOperatorOverloader();

    public StandardEvaluationContext()
    {
        RootObject = TypedValue.Null;
    }

    public StandardEvaluationContext(object rootObject)
    {
        RootObject = new TypedValue(rootObject);
    }

    public ITypedValue RootObject { get; private set; }

    public IServiceResolver ServiceResolver { get; set; }

    public List<IPropertyAccessor> PropertyAccessors
    {
        get
        {
            _propertyAccessors = InitPropertyAccessors();
            return _propertyAccessors;
        }

        set
        {
            _propertyAccessors = value;
        }
    }

    public List<IConstructorResolver> ConstructorResolvers
    {
        get
        {
            _constructorResolvers = InitConstructorResolvers();
            return _constructorResolvers;
        }

        set
        {
            _constructorResolvers = value;
        }
    }

    public List<IMethodResolver> MethodResolvers
    {
        get
        {
            _methodResolvers = InitMethodResolvers();
            return _methodResolvers;
        }

        set
        {
            _methodResolvers = value;
        }
    }

    public ITypeLocator TypeLocator
    {
        get
        {
            _typeLocator ??= new StandardTypeLocator();
            return _typeLocator;
        }

        set
        {
            _typeLocator = value ?? throw new ArgumentNullException("TypeLocator can not be null");
        }
    }

    public ITypeConverter TypeConverter
    {
        get
        {
            _typeConverter ??= new StandardTypeConverter();
            return _typeConverter;
        }

        set
        {
            _typeConverter = value ?? throw new ArgumentNullException("TypeConverter can not be null");
        }
    }

    public ITypeComparator TypeComparator
    {
        get => _typeComparator;

        set
        {
            _typeComparator = value ?? throw new ArgumentNullException("TypeComparator can not be null");
        }
    }

    public IOperatorOverloader OperatorOverloader
    {
        get => _operatorOverloader;

        set
        {
            _operatorOverloader = value ?? throw new ArgumentNullException("OperatorOverloader can not be null");
        }
    }

    public void SetVariable(string name, object value)
    {
        // For backwards compatibility, we ignore null names here...
        // And since ConcurrentHashMap cannot store null values, we simply take null
        // as a remove from the Map (with the same result from lookupVariable below).
        if (name != null)
        {
            if (value != null)
            {
                _variables[name] = value;
            }
            else
            {
                _variables.TryRemove(name, out _);
            }
        }
    }

    public void SetVariables(Dictionary<string, object> variables)
    {
        foreach (var v in variables)
        {
            SetVariable(v.Key, v.Value);
        }
    }

    public void RegisterFunction(string name, MethodInfo method)
    {
        _variables[name] = method;
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

    public void SetRootObject(object rootObject, Type typeDescriptor)
    {
        RootObject = new TypedValue(rootObject, typeDescriptor);
    }

    public void SetRootObject(object rootObject)
    {
        RootObject = rootObject != null ? new TypedValue(rootObject) : TypedValue.Null;
    }

    public void AddPropertyAccessor(IPropertyAccessor accessor)
    {
        AddBeforeDefault(InitPropertyAccessors(), accessor);
    }

    public bool RemovePropertyAccessor(IPropertyAccessor accessor)
    {
        return InitPropertyAccessors().Remove(accessor);
    }

    public void AddConstructorResolver(IConstructorResolver accessor)
    {
        AddBeforeDefault(InitConstructorResolvers(), accessor);
    }

    public bool RemoveConstructorResolver(IConstructorResolver accessor)
    {
        return InitConstructorResolvers().Remove(accessor);
    }

    public void AddMethodResolver(IMethodResolver accessor)
    {
        AddBeforeDefault(InitMethodResolvers(), accessor);
    }

    public bool RemoveMethodResolver(IMethodResolver accessor)
    {
        return InitMethodResolvers().Remove(accessor);
    }

    public void RegisterMethodFilter(Type type, IMethodFilter filter)
    {
        InitMethodResolvers();
        var resolver = _reflectiveMethodResolver;
        if (resolver == null)
        {
            throw new InvalidOperationException("Method filter cannot be set as the reflective method resolver is not in use");
        }

        resolver.RegisterMethodFilter(type, filter);
    }

    private static void AddBeforeDefault<T>(List<T> resolvers, T resolver)
    {
        resolvers.Insert(resolvers.Count - 1, resolver);
    }

    private List<IPropertyAccessor> InitPropertyAccessors()
    {
        var accessors = _propertyAccessors;
        if (accessors == null)
        {
            accessors = new List<IPropertyAccessor>(5)
            {
                new ReflectivePropertyAccessor()
            };
            _propertyAccessors = accessors;
        }

        return accessors;
    }

    private List<IConstructorResolver> InitConstructorResolvers()
    {
        var resolvers = _constructorResolvers;
        if (resolvers == null)
        {
            resolvers = new List<IConstructorResolver>(1)
            {
                new ReflectiveConstructorResolver()
            };
            _constructorResolvers = resolvers;
        }

        return resolvers;
    }

    private List<IMethodResolver> InitMethodResolvers()
    {
        var resolvers = _methodResolvers;
        if (resolvers == null)
        {
            resolvers = new List<IMethodResolver>(1);
            _reflectiveMethodResolver = new ReflectiveMethodResolver();
            resolvers.Add(_reflectiveMethodResolver);
            _methodResolvers = resolvers;
        }

        return resolvers;
    }
}
