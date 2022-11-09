// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Steeltoe.Common.Util;

namespace Steeltoe.Common.Expression.Internal.Spring.Support;

public class ReflectivePropertyAccessor : IPropertyAccessor
{
    private readonly bool _allowWrite;
    private readonly ConcurrentDictionary<PropertyCacheKey, InvokerPair> _readerCache = new();
    private readonly ConcurrentDictionary<PropertyCacheKey, MemberInfo> _writerCache = new();
    private readonly ConcurrentDictionary<PropertyCacheKey, Type> _typeDescriptorCache = new();

    public ReflectivePropertyAccessor()
    {
        _allowWrite = true;
    }

    public ReflectivePropertyAccessor(bool allowWrite)
    {
        _allowWrite = allowWrite;
    }

    public virtual IList<Type> GetSpecificTargetClasses()
    {
        return null;
    }

    public bool CanRead(IEvaluationContext context, object target, string name)
    {
        if (target == null)
        {
            return false;
        }

        Type type = target as Type ?? target.GetType();

        if (type.IsArray && name == "Length")
        {
            return true;
        }

        var cacheKey = new PropertyCacheKey(type, name, target is Type);

        if (_readerCache.ContainsKey(cacheKey))
        {
            return true;
        }

        MethodInfo method = FindGetterForProperty(name, type, target);

        if (method != null)
        {
            // Treat it like a property...
            // The readerCache will only contain gettable properties (let's not worry about setters for now).
            Type typeDescriptor = method.ReturnType;
            method = ClassUtils.GetInterfaceMethodIfPossible(method);
            _readerCache[cacheKey] = new InvokerPair(method, typeDescriptor);
            _typeDescriptorCache[cacheKey] = typeDescriptor;
            return true;
        }

        FieldInfo field = FindField(name, type, target);

        if (field != null)
        {
            Type typeDescriptor = field.FieldType;
            _readerCache[cacheKey] = new InvokerPair(field, typeDescriptor);
            _typeDescriptorCache[cacheKey] = typeDescriptor;
            return true;
        }

        return false;
    }

    public ITypedValue Read(IEvaluationContext context, object target, string name)
    {
        ArgumentGuard.NotNull(target);

        Type type = target as Type ?? target.GetType();

        if (type.IsArray && name == "Length")
        {
            if (target is Type)
            {
                throw new AccessException("Cannot access length on array class itself");
            }

            var asArray = (Array)target;
            return new TypedValue(asArray.GetLength(0));
        }

        var cacheKey = new PropertyCacheKey(type, name, target is Type);
        _readerCache.TryGetValue(cacheKey, out InvokerPair invoker);

        if (invoker == null || invoker.Member is MethodInfo)
        {
            var method = (MethodInfo)invoker?.Member;

            if (method == null)
            {
                method = FindGetterForProperty(name, type, target);

                if (method != null)
                {
                    // Treat it like a property...
                    // The readerCache will only contain gettable properties (let's not worry about setters for now).
                    Type typeDescriptor = method.ReturnType;
                    method = ClassUtils.GetInterfaceMethodIfPossible(method);
                    invoker = new InvokerPair(method, typeDescriptor);
                    _readerCache[cacheKey] = invoker;
                }
            }

            if (method != null)
            {
                try
                {
                    object value = method.Invoke(target, Array.Empty<object>());
                    return new TypedValue(value, value != null ? value.GetType() : invoker.TypeDescriptor);
                }
                catch (Exception ex)
                {
                    throw new AccessException($"Unable to access property '{name}' through getter method", ex);
                }
            }
        }

        if (invoker == null || invoker.Member is FieldInfo)
        {
            var field = (FieldInfo)invoker?.Member;

            if (field == null)
            {
                field = FindField(name, type, target);

                if (field != null)
                {
                    invoker = new InvokerPair(field, field.FieldType);
                    _readerCache[cacheKey] = invoker;
                }
            }

            if (field != null)
            {
                try
                {
                    object value = field.GetValue(target);
                    return new TypedValue(value, value != null ? value.GetType() : invoker.TypeDescriptor);
                }
                catch (Exception ex)
                {
                    throw new AccessException($"Unable to access field '{name}'", ex);
                }
            }
        }

        throw new AccessException($"Neither getter method nor field found for property '{name}'");
    }

    public bool CanWrite(IEvaluationContext context, object target, string name)
    {
        if (!_allowWrite || target == null)
        {
            return false;
        }

        Type type = target as Type ?? target.GetType();
        var cacheKey = new PropertyCacheKey(type, name, target is Type);

        if (_writerCache.ContainsKey(cacheKey))
        {
            return true;
        }

        MethodInfo method = FindSetterForProperty(name, type, target);

        if (method != null)
        {
            // Treat it like a property
            Type typeDescriptor = method.GetParameters()[0].ParameterType;
            method = ClassUtils.GetInterfaceMethodIfPossible(method);
            _writerCache[cacheKey] = method;
            _typeDescriptorCache[cacheKey] = typeDescriptor;
            return true;
        }

        FieldInfo field = FindField(name, type, target);

        if (field != null)
        {
            _writerCache[cacheKey] = field;
            _typeDescriptorCache[cacheKey] = field.FieldType;
            return true;
        }

        return false;
    }

    public void Write(IEvaluationContext context, object target, string name, object newValue)
    {
        ArgumentGuard.NotNull(target);

        if (!_allowWrite)
        {
            throw new AccessException($"PropertyAccessor for property '{name}' on target [{target}] does not allow write operations");
        }

        Type type = target as Type ?? target.GetType();

        object possiblyConvertedNewValue = newValue;
        Type typeDescriptor = GetTypeDescriptor(context, target, name);

        if (typeDescriptor != null)
        {
            try
            {
                possiblyConvertedNewValue = context.TypeConverter.ConvertValue(newValue, newValue?.GetType(), typeDescriptor);
            }
            catch (EvaluationException evaluationException)
            {
                throw new AccessException("Type conversion failure", evaluationException);
            }
        }

        var cacheKey = new PropertyCacheKey(type, name, target is Type);
        _writerCache.TryGetValue(cacheKey, out MemberInfo cachedMember);

        if (cachedMember == null || cachedMember is MethodInfo)
        {
            var method = (MethodInfo)cachedMember;

            if (method == null)
            {
                method = FindSetterForProperty(name, type, target);

                if (method != null)
                {
                    method = ClassUtils.GetInterfaceMethodIfPossible(method);
                    cachedMember = method;
                    _writerCache[cacheKey] = cachedMember;
                }
            }

            if (method != null)
            {
                try
                {
                    method.Invoke(target, new[]
                    {
                        possiblyConvertedNewValue
                    });

                    return;
                }
                catch (Exception ex)
                {
                    throw new AccessException($"Unable to access property '{name}' through setter method", ex);
                }
            }
        }

        if (cachedMember == null || cachedMember is FieldInfo)
        {
            var field = (FieldInfo)cachedMember;

            if (field == null)
            {
                field = FindField(name, type, target);

                if (field != null)
                {
                    cachedMember = field;
                    _writerCache[cacheKey] = cachedMember;
                }
            }

            if (field != null)
            {
                try
                {
                    field.SetValue(target, possiblyConvertedNewValue);
                    return;
                }
                catch (Exception ex)
                {
                    throw new AccessException($"Unable to access field '{name}'", ex);
                }
            }
        }

        throw new AccessException($"Neither setter method nor field found for property '{name}'");
    }

    public IPropertyAccessor CreateOptimalAccessor(IEvaluationContext context, object target, string name)
    {
        // Don't be clever for arrays or a null target...
        if (target == null)
        {
            return this;
        }

        Type type = target as Type ?? target.GetType();

        if (type.IsArray)
        {
            return this;
        }

        var cacheKey = new PropertyCacheKey(type, name, target is Type);
        _readerCache.TryGetValue(cacheKey, out InvokerPair invocationTarget);

        if (invocationTarget == null || invocationTarget.Member is MethodInfo)
        {
            var method = (MethodInfo)invocationTarget?.Member;

            if (method == null)
            {
                method = FindGetterForProperty(name, type, target);

                if (method != null)
                {
                    Type typeDescriptor = method.ReturnType;
                    method = ClassUtils.GetInterfaceMethodIfPossible(method);
                    invocationTarget = new InvokerPair(method, typeDescriptor);
                    _readerCache[cacheKey] = invocationTarget;
                }
            }

            if (method != null)
            {
                return new OptimalPropertyAccessor(invocationTarget);
            }
        }

        if (invocationTarget == null || invocationTarget.Member is FieldInfo)
        {
            FieldInfo field = invocationTarget != null ? (FieldInfo)invocationTarget.Member : null;

            if (field == null)
            {
                field = FindField(name, type, target is Type);

                if (field != null)
                {
                    invocationTarget = new InvokerPair(field, field.FieldType);
                    _readerCache[cacheKey] = invocationTarget;
                }
            }

            if (field != null)
            {
                return new OptimalPropertyAccessor(invocationTarget);
            }
        }

        return this;
    }

    internal static string Capitalize(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        char baseChar = str[0];
        char updatedChar = char.ToUpperInvariant(baseChar);

        if (baseChar == updatedChar)
        {
            return str;
        }

        char[] chars = str.ToCharArray();
        chars[0] = updatedChar;
        return new string(chars);
    }

    protected virtual MethodInfo FindGetterForProperty(string propertyName, Type type, bool mustBeStatic)
    {
        return FindMethodForProperty(propertyName, type, false, mustBeStatic);
    }

    protected virtual MethodInfo FindSetterForProperty(string propertyName, Type type, bool mustBeStatic)
    {
        return FindMethodForProperty(propertyName, type, true, mustBeStatic);
    }

    protected virtual bool IsCandidateForProperty(MethodInfo method, Type targetClass)
    {
        return true;
    }

    protected virtual FieldInfo FindField(string name, Type type, bool mustBeStatic)
    {
        FieldInfo[] fields = type.GetFields();

        foreach (FieldInfo field in fields)
        {
            if (field.Name == name && (!mustBeStatic || field.IsStatic))
            {
                return field;
            }
        }

        // We'll search superclasses and implemented interfaces explicitly,
        // although it shouldn't be necessary - however, see SPR-10125.
        if (type.BaseType != null)
        {
            FieldInfo field = FindField(name, type.BaseType, mustBeStatic);

            if (field != null)
            {
                return field;
            }
        }

        foreach (Type implementedInterface in type.GetInterfaces())
        {
            FieldInfo field = FindField(name, implementedInterface, mustBeStatic);

            if (field != null)
            {
                return field;
            }
        }

        return null;
    }

    private FieldInfo FindField(string name, Type type, object target)
    {
        FieldInfo field = FindField(name, type, target is Type);

        if (field == null && target is Type)
        {
            field = FindField(name, target.GetType(), false);
        }

        return field;
    }

    private MethodInfo FindMethodForProperty(string propertyName, Type type, bool setter, bool mustBeStatic)
    {
        PropertyInfo propInfo = type.GetProperty(propertyName);

        if (propInfo != null)
        {
            MethodInfo method = null;

            if (setter)
            {
                if (propInfo.CanWrite)
                {
                    method = propInfo.GetSetMethod();
                }
            }
            else
            {
                if (propInfo.CanRead)
                {
                    method = propInfo.GetGetMethod();
                }
            }

            if (method != null && IsCandidateForProperty(method, type) && (!mustBeStatic || method.IsStatic))
            {
                return method;
            }
        }

        return null;
    }

    private Type GetTypeDescriptor(IEvaluationContext context, object target, string name)
    {
        Type type = target as Type ?? target.GetType();

        if (type.IsArray && name == "Length")
        {
            return typeof(int);
        }

        var cacheKey = new PropertyCacheKey(type, name, target is Type);
        _typeDescriptorCache.TryGetValue(cacheKey, out Type typeDescriptor);

        if (typeDescriptor == null)
        {
            // Attempt to populate the cache entry
            try
            {
                if (CanRead(context, target, name) || CanWrite(context, target, name))
                {
                    _typeDescriptorCache.TryGetValue(cacheKey, out typeDescriptor);
                }
            }
            catch (AccessException)
            {
                // Continue with null type descriptor
            }
        }

        return typeDescriptor;
    }

    private MethodInfo FindGetterForProperty(string propertyName, Type type, object target)
    {
        MethodInfo method = FindGetterForProperty(propertyName, type, target is Type);

        if (method == null && target is Type)
        {
            method = FindGetterForProperty(propertyName, typeof(Type), false);
        }

        return method;
    }

    private MethodInfo FindSetterForProperty(string propertyName, Type type, object target)
    {
        MethodInfo method = FindSetterForProperty(propertyName, type, target is Type);

        if (method == null && target is Type)
        {
            method = FindSetterForProperty(propertyName, typeof(Type), false);
        }

        return method;
    }

    public class InvokerPair
    {
        public Type TypeDescriptor { get; }

        public MemberInfo Member { get; }

        public InvokerPair(MemberInfo member, Type typeDescriptor)
        {
            Member = member;
            TypeDescriptor = typeDescriptor;
        }
    }

#pragma warning disable S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
    public class PropertyCacheKey : IComparable<PropertyCacheKey>
#pragma warning restore S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
    {
        private readonly Type _type;
        private readonly string _property;
        private readonly bool _targetIsClass;

        public PropertyCacheKey(Type type, string name, bool targetIsClass)
        {
            _type = type;
            _property = name;
            _targetIsClass = targetIsClass;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not PropertyCacheKey otherKey)
            {
                return false;
            }

            return _type == otherKey._type && _property == otherKey._property && _targetIsClass == otherKey._targetIsClass;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_type, _property);
        }

        public override string ToString()
        {
            return $"CacheKey [type={_type.FullName}, property={_property}, {_property}, targetIsClass={_targetIsClass}]";
        }

        public int CompareTo(PropertyCacheKey other)
        {
            int result = string.Compare(_type.Name, _type.Name, StringComparison.Ordinal);

            if (result == 0)
            {
                result = string.Compare(_property, other._property, StringComparison.Ordinal);
            }

            return result;
        }
    }

    public class OptimalPropertyAccessor : ICompilablePropertyAccessor
    {
        private static readonly ISet<Type> NumericIntegralTypes = new[]
        {
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort),
            typeof(byte),
            typeof(sbyte),
            typeof(char)
        }.ToHashSet();

        public MemberInfo Member { get; }

        public Type TypeDescriptor { get; }

        public OptimalPropertyAccessor(InvokerPair target)
        {
            Member = target.Member;
            TypeDescriptor = target.TypeDescriptor;
        }

        public IList<Type> GetSpecificTargetClasses()
        {
            throw new InvalidOperationException("Should not be called on an OptimalPropertyAccessor");
        }

        public bool CanRead(IEvaluationContext context, object target, string name)
        {
            if (target == null)
            {
                return false;
            }

            Type type = target as Type ?? target.GetType();

            if (type.IsArray)
            {
                return false;
            }

            if (Member is MethodInfo method)
            {
                string getterName = $"get_{Capitalize(name)}";

                if (getterName == method.Name)
                {
                    return true;
                }

                return false;
            }

            var field = (FieldInfo)Member;
            return field.Name == name;
        }

        public ITypedValue Read(IEvaluationContext context, object target, string name)
        {
            if (Member is MethodInfo method)
            {
                try
                {
                    object value = method.Invoke(target, Array.Empty<object>());
                    return new TypedValue(value, value != null ? value.GetType() : TypeDescriptor);
                }
                catch (Exception ex)
                {
                    throw new AccessException($"Unable to access property '{name}' through getter method", ex);
                }
            }

            var field = (FieldInfo)Member;

            try
            {
                object value = field.GetValue(target);
                return new TypedValue(value, value != null ? value.GetType() : TypeDescriptor);
            }
            catch (Exception ex)
            {
                throw new AccessException($"Unable to access field '{name}'", ex);
            }
        }

        public bool CanWrite(IEvaluationContext context, object target, string name)
        {
            throw new InvalidOperationException("Should not be called on an OptimalPropertyAccessor");
        }

        public void Write(IEvaluationContext context, object target, string name, object newValue)
        {
            throw new InvalidOperationException("Should not be called on an OptimalPropertyAccessor");
        }

        public bool IsCompilable()
        {
            if (!ReflectionHelper.IsPublic(Member.DeclaringType))
            {
                return false;
            }

            if (Member is MethodInfo info)
            {
                return info.IsPublic;
            }

            return ((FieldInfo)Member).IsPublic;
        }

        public Type GetPropertyType()
        {
            if (Member is MethodInfo info)
            {
                return info.ReturnType;
            }

            return ((FieldInfo)Member).FieldType;
        }

        public void GenerateCode(string propertyName, ILGenerator gen, CodeFlow cf)
        {
            if (Member is MethodInfo info)
            {
                GenerateCode(info, gen, cf);
            }
            else
            {
                GenerateCode((FieldInfo)Member, gen, cf);
            }
        }

        private void GenerateCode(MethodInfo method, ILGenerator gen, CodeFlow cf)
        {
            TypeDescriptor stackDescriptor = cf.LastDescriptor();

            if (stackDescriptor == null)
            {
                CodeFlow.LoadTarget(gen);
                stackDescriptor = Spring.TypeDescriptor.Object;
            }

            if (!method.IsStatic)
            {
                // Instance
                if (method.DeclaringType.IsValueType)
                {
                    if (stackDescriptor != null && stackDescriptor.IsBoxed)
                    {
                        gen.Emit(OpCodes.Unbox_Any, method.DeclaringType);
                    }

                    LocalBuilder vtLocal = gen.DeclareLocal(method.DeclaringType);
                    gen.Emit(OpCodes.Stloc, vtLocal);
                    gen.Emit(OpCodes.Ldloca, vtLocal);
                    gen.Emit(OpCodes.Call, method);
                }
                else
                {
                    if (stackDescriptor == null || method.DeclaringType != stackDescriptor.Value)
                    {
                        gen.Emit(OpCodes.Castclass, method.DeclaringType);
                    }

                    gen.Emit(OpCodes.Callvirt, method);
                }
            }
            else
            {
                // Static
                if (stackDescriptor != null)
                {
                    // A static field/method call will not consume what is on the stack,
                    // it needs to be popped off.
                    gen.Emit(OpCodes.Pop);
                }

                gen.Emit(OpCodes.Call, method);
            }
        }

        private void GenerateCode(FieldInfo field, ILGenerator gen, CodeFlow cf)
        {
            TypeDescriptor stackDescriptor = cf.LastDescriptor();

            if (stackDescriptor == null)
            {
                CodeFlow.LoadTarget(gen);
                stackDescriptor = Spring.TypeDescriptor.Object;
            }

            if (!field.IsStatic)
            {
                // Instance
                if (field.DeclaringType.IsValueType)
                {
                    if (stackDescriptor != null && stackDescriptor.IsBoxed)
                    {
                        gen.Emit(OpCodes.Unbox_Any, field.DeclaringType);
                    }
                }
                else
                {
                    if (stackDescriptor == null || field.DeclaringType != stackDescriptor.Value)
                    {
                        gen.Emit(OpCodes.Castclass, field.DeclaringType);
                    }
                }

                gen.Emit(OpCodes.Ldfld, field);
            }
            else
            {
                // Static
                if (stackDescriptor != null)
                {
                    // A static field/method call will not consume what is on the stack,
                    // it needs to be popped off.
                    gen.Emit(OpCodes.Pop);
                }

                if (field.IsLiteral)
                {
                    EmitLiteralFieldCode(gen, field);
                }
                else
                {
                    gen.Emit(OpCodes.Ldsfld, field);
                }
            }
        }

        private void EmitLiteralFieldCode(ILGenerator gen, FieldInfo field)
        {
            object constant = field.GetRawConstantValue();

            if (field.FieldType.IsClass && constant == null)
            {
                gen.Emit(OpCodes.Ldnull);
                return;
            }

            if (constant == null)
            {
                return;
            }

            switch (field.FieldType)
            {
                case var type when NumericIntegralTypes.Contains(type):
                    gen.Emit(OpCodes.Ldc_I4, (int)constant);
                    return;
                case var type when type == typeof(long) || type == typeof(ulong):
                    gen.Emit(OpCodes.Ldc_I8, (long)constant);
                    return;
                case var type when type == typeof(float):
                    gen.Emit(OpCodes.Ldc_R4, (float)constant);
                    return;
                case var type when type == typeof(double):
                    gen.Emit(OpCodes.Ldc_R8, (double)constant);
                    return;
                case var type when type == typeof(string):
                    gen.Emit(OpCodes.Ldstr, (string)constant);
                    return;
                default:
                    return;
            }
        }
    }
}
