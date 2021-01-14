﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Support
{
    public class ReflectivePropertyAccessor : IPropertyAccessor
    {
        private readonly bool _allowWrite;
        private readonly ConcurrentDictionary<PropertyCacheKey, InvokerPair> _readerCache = new ConcurrentDictionary<PropertyCacheKey, InvokerPair>();
        private readonly ConcurrentDictionary<PropertyCacheKey, MemberInfo> _writerCache = new ConcurrentDictionary<PropertyCacheKey, MemberInfo>();
        private readonly ConcurrentDictionary<PropertyCacheKey, Type> _typeDescriptorCache = new ConcurrentDictionary<PropertyCacheKey, Type>();
        private volatile InvokerPair _lastReadInvokerPair;

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

            var type = target is Type ? (Type)target : target.GetType();
            if (type.IsArray && name.Equals("Length"))
            {
                return true;
            }

            var cacheKey = new PropertyCacheKey(type, name, target is Type);
            if (_readerCache.ContainsKey(cacheKey))
            {
                return true;
            }

            var method = FindGetterForProperty(name, type, target);
            if (method != null)
            {
                // Treat it like a property...
                // The readerCache will only contain gettable properties (let's not worry about setters for now).
                var typeDescriptor = method.ReturnType;
                method = ClassUtils.GetInterfaceMethodIfPossible(method);
                _readerCache[cacheKey] = new InvokerPair(method, typeDescriptor);
                _typeDescriptorCache[cacheKey] = typeDescriptor;
                return true;
            }
            else
            {
                var field = FindField(name, type, target);
                if (field != null)
                {
                    var typeDescriptor = field.FieldType;
                    _readerCache[cacheKey] = new InvokerPair(field, typeDescriptor);
                    _typeDescriptorCache[cacheKey] = typeDescriptor;
                    return true;
                }
            }

            return false;
        }

        public ITypedValue Read(IEvaluationContext context, object target, string name)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var type = target is Type ? (Type)target : target.GetType();

            if (type.IsArray && name.Equals("Length"))
            {
                if (target is Type)
                {
                    throw new AccessException("Cannot access length on array class itself");
                }

                var asArray = (Array)target;
                return new TypedValue(asArray.GetLength(0));
            }

            var cacheKey = new PropertyCacheKey(type, name, target is Type);
            _readerCache.TryGetValue(cacheKey, out var invoker);
            _lastReadInvokerPair = invoker;

            if (invoker == null || invoker.Member is MethodInfo)
            {
                var method = (MethodInfo)(invoker != null ? invoker.Member : null);
                if (method == null)
                {
                    method = FindGetterForProperty(name, type, target);
                    if (method != null)
                    {
                        // Treat it like a property...
                        // The readerCache will only contain gettable properties (let's not worry about setters for now).
                        var typeDescriptor = method.ReturnType;
                        method = ClassUtils.GetInterfaceMethodIfPossible(method);
                        invoker = new InvokerPair(method, typeDescriptor);
                        _lastReadInvokerPair = invoker;
                        _readerCache[cacheKey] = invoker;
                    }
                }

                if (method != null)
                {
                    try
                    {
                        var value = method.Invoke(target, new object[0]);
                        return new TypedValue(value, value != null ? value.GetType() : invoker.TypeDescriptor);
                    }
                    catch (Exception ex)
                    {
                        throw new AccessException("Unable to access property '" + name + "' through getter method", ex);
                    }
                }
            }

            if (invoker == null || invoker.Member is FieldInfo)
            {
                var field = (FieldInfo)(invoker == null ? null : invoker.Member);
                if (field == null)
                {
                    field = FindField(name, type, target);
                    if (field != null)
                    {
                        invoker = new InvokerPair(field, field.FieldType);
                        _lastReadInvokerPair = invoker;
                        _readerCache[cacheKey] = invoker;
                    }
                }

                if (field != null)
                {
                    try
                    {
                        var value = field.GetValue(target);
                        return new TypedValue(value, value != null ? value.GetType() : invoker.TypeDescriptor);
                    }
                    catch (Exception ex)
                    {
                        throw new AccessException("Unable to access field '" + name + "'", ex);
                    }
                }
            }

            throw new AccessException("Neither getter method nor field found for property '" + name + "'");
        }

        public bool CanWrite(IEvaluationContext context, object target, string name)
        {
            if (!_allowWrite || target == null)
            {
                return false;
            }

            var type = target is Type ? (Type)target : target.GetType();
            var cacheKey = new PropertyCacheKey(type, name, target is Type);
            if (_writerCache.ContainsKey(cacheKey))
            {
                return true;
            }

            var method = FindSetterForProperty(name, type, target);
            if (method != null)
            {
                // Treat it like a property
                var typeDescriptor = method.GetParameters()[0].ParameterType;
                method = ClassUtils.GetInterfaceMethodIfPossible(method);
                _writerCache[cacheKey] = method;
                _typeDescriptorCache[cacheKey] = typeDescriptor;
                return true;
            }
            else
            {
                var field = FindField(name, type, target);
                if (field != null)
                {
                    _writerCache[cacheKey] = field;
                    _typeDescriptorCache[cacheKey] = field.FieldType;
                    return true;
                }
            }

            return false;
        }

        public void Write(IEvaluationContext context, object target, string name, object newValue)
        {
            if (!_allowWrite)
            {
                throw new AccessException("PropertyAccessor for property '" + name + "' on target [" + target + "] does not allow write operations");
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var type = target is Type ? (Type)target : target.GetType();

            var possiblyConvertedNewValue = newValue;
            var typeDescriptor = GetTypeDescriptor(context, target, name);
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
            _writerCache.TryGetValue(cacheKey, out var cachedMember);

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
                        method.Invoke(target, new object[] { possiblyConvertedNewValue });
                        return;
                    }
                    catch (Exception ex)
                    {
                        throw new AccessException("Unable to access property '" + name + "' through setter method", ex);
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
                        throw new AccessException("Unable to access field '" + name + "'", ex);
                    }
                }
            }

            throw new AccessException("Neither setter method nor field found for property '" + name + "'");
        }

        public IPropertyAccessor CreateOptimalAccessor(IEvaluationContext context, object target, string name)
        {
            // Don't be clever for arrays or a null target...
            if (target == null)
            {
                return this;
            }

            var clazz = target is Type ? (Type)target : target.GetType();
            if (clazz.IsArray)
            {
                return this;
            }

            var cacheKey = new PropertyCacheKey(clazz, name, target is Type);
            _readerCache.TryGetValue(cacheKey, out var invocationTarget);

            if (invocationTarget == null || invocationTarget.Member is MethodInfo)
            {
                var method = (MethodInfo)invocationTarget?.Member;
                if (method == null)
                {
                    method = FindGetterForProperty(name, clazz, target);
                    if (method != null)
                    {
                        var typeDescriptor = method.ReturnType;
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
                var field = invocationTarget != null ? (FieldInfo)invocationTarget.Member : null;
                if (field == null)
                {
                    field = FindField(name, clazz, target is Type);
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

            var baseChar = str[0];
            var updatedChar = char.ToUpperInvariant(baseChar);
            if (baseChar == updatedChar)
            {
                return str;
            }

            var chars = str.ToCharArray();
            chars[0] = updatedChar;
            return new string(chars);
        }

        protected virtual MethodInfo FindGetterForProperty(string propertyName, Type clazz, bool mustBeStatic)
        {
            return FindMethodForProperty(propertyName, clazz, false, mustBeStatic);
        }

        protected virtual MethodInfo FindSetterForProperty(string propertyName, Type clazz, bool mustBeStatic)
        {
            return FindMethodForProperty(propertyName, clazz, true, mustBeStatic);
        }

        protected virtual bool IsCandidateForProperty(MethodInfo method, Type targetClass)
        {
            return true;
        }

        protected virtual FieldInfo FindField(string name, Type clazz, bool mustBeStatic)
        {
            var fields = clazz.GetFields();
            foreach (var field in fields)
            {
                if (field.Name.Equals(name) && (!mustBeStatic || field.IsStatic))
                {
                    return field;
                }
            }

            // We'll search superclasses and implemented interfaces explicitly,
            // although it shouldn't be necessary - however, see SPR-10125.
            if (clazz.BaseType != null)
            {
                var field = FindField(name, clazz.BaseType, mustBeStatic);
                if (field != null)
                {
                    return field;
                }
            }

            foreach (var implementedInterface in clazz.GetInterfaces())
            {
                var field = FindField(name, implementedInterface, mustBeStatic);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private FieldInfo FindField(string name, Type clazz, object target)
        {
            var field = FindField(name, clazz, target is Type);
            if (field == null && target is Type)
            {
                field = FindField(name, target.GetType(), false);
            }

            return field;
        }

        private MethodInfo FindMethodForProperty(string propertyName, Type clazz, bool setter, bool mustBeStatic)
        {
            var propInfo = clazz.GetProperty(propertyName);
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

                if (method != null && IsCandidateForProperty(method, clazz) && (!mustBeStatic || method.IsStatic))
                {
                    return method;
                }
            }

            return null;
        }

        private Type GetTypeDescriptor(IEvaluationContext context, object target, string name)
        {
            var type = target is Type type1 ? type1 : target.GetType();

            if (type.IsArray && name.Equals("Length"))
            {
                return typeof(int);
            }

            var cacheKey = new PropertyCacheKey(type, name, target is Type);
            _typeDescriptorCache.TryGetValue(cacheKey, out var typeDescriptor);
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
                catch (AccessException ex)
                {
                    // Continue with null type descriptor
                }
            }

            return typeDescriptor;
        }

        private MethodInfo FindGetterForProperty(string propertyName, Type clazz, object target)
        {
            var method = FindGetterForProperty(propertyName, clazz, target is Type);
            if (method == null && target is Type)
            {
                method = FindGetterForProperty(propertyName, target.GetType(), false);
            }

            return method;
        }

        private MethodInfo FindSetterForProperty(string propertyName, Type clazz, object target)
        {
            var method = FindSetterForProperty(propertyName, clazz, target is Type);
            if (method == null && target is Type)
            {
                method = FindSetterForProperty(propertyName, target.GetType(), false);
            }

            return method;
        }

        public class InvokerPair
        {
            private readonly MemberInfo _member;

            private readonly Type _typeDescriptor;

            public Type TypeDescriptor => _typeDescriptor;

            public MemberInfo Member => _member;

            public InvokerPair(MemberInfo member, Type typeDescriptor)
            {
                _member = member;
                _typeDescriptor = typeDescriptor;
            }
        }

#pragma warning disable S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
        public class PropertyCacheKey : IComparable<PropertyCacheKey>
#pragma warning restore S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
        {
            private readonly Type _clazz;
            private readonly string _property;
            private bool _targetIsClass;

            public PropertyCacheKey(Type clazz, string name, bool targetIsClass)
            {
                _clazz = clazz;
                _property = name;
                _targetIsClass = targetIsClass;
            }

            public override bool Equals(object other)
            {
                if (this == other)
                {
                    return true;
                }

                if (!(other is PropertyCacheKey))
                {
                    return false;
                }

                var otherKey = (PropertyCacheKey)other;
                return _clazz == otherKey._clazz && _property.Equals(otherKey._property) &&
                        _targetIsClass == otherKey._targetIsClass;
            }

            public override int GetHashCode()
            {
                return (_clazz.GetHashCode() * 29) + _property.GetHashCode();
            }

            public override string ToString()
            {
                return "CacheKey [clazz=" + _clazz.FullName + ", property=" + _property + ", " +
                        _property + ", targetIsClass=" + _targetIsClass + "]";
            }

            public int CompareTo(PropertyCacheKey other)
            {
                var result = _clazz.Name.CompareTo(_clazz.Name);
                if (result == 0)
                {
                    result = _property.CompareTo(other._property);
                }

                return result;
            }
        }

        public class OptimalPropertyAccessor : ICompilablePropertyAccessor
        {
            private readonly MemberInfo _member;

            private readonly Type _typeDescriptor;

            public OptimalPropertyAccessor(InvokerPair target)
            {
                _member = target.Member;
                _typeDescriptor = target.TypeDescriptor;
            }

            public MemberInfo Member => _member;

            public Type TypeDescriptor => _typeDescriptor;

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

                var type = target is Type ? (Type)target : target.GetType();
                if (type.IsArray)
                {
                    return false;
                }

                if (_member is MethodInfo)
                {
                    var method = (MethodInfo)_member;
                    var getterName = "get_" + ReflectivePropertyAccessor.Capitalize(name);
                    if (getterName.Equals(method.Name))
                    {
                        return true;
                    }

                    return false;
                }
                else
                {
                    var field = (FieldInfo)_member;
                    return field.Name.Equals(name);
                }
            }

            public ITypedValue Read(IEvaluationContext context, object target, string name)
            {
                if (_member is MethodInfo)
                {
                    var method = (MethodInfo)_member;
                    try
                    {
                        var value = method.Invoke(target, new object[0]);
                        return new TypedValue(value, (value != null) ? value.GetType() : _typeDescriptor);
                    }
                    catch (Exception ex)
                    {
                        throw new AccessException("Unable to access property '" + name + "' through getter method", ex);
                    }
                }
                else
                {
                    var field = (FieldInfo)_member;
                    try
                    {
                        var value = field.GetValue(target);
                        return new TypedValue(value, (value != null) ? value.GetType() : _typeDescriptor);
                    }
                    catch (Exception ex)
                    {
                        throw new AccessException("Unable to access field '" + name + "'", ex);
                    }
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
                if (!_member.DeclaringType.IsPublic)
                {
                    return false;
                }

                if (_member is MethodInfo)
                {
                    return ((MethodInfo)_member).IsPublic;
                }
                else
                {
                    return ((FieldInfo)_member).IsPublic;
                }
            }

            public Type GetPropertyType()
            {
                if (_member is MethodInfo)
                {
                    return ((MethodInfo)_member).ReturnType;
                }
                else
                {
                    return ((FieldInfo)_member).FieldType;
                }
            }

            public void GenerateCode(string propertyName, DynamicMethod mv, CodeFlow cf)
            {
                // bool isStatic = Modifier.isStatic(this.member.getModifiers());
                //    String descriptor = cf.lastDescriptor();
                //    String classDesc = this.member.getDeclaringClass().getName().replace('.', '/');

                // if (!isStatic)
                //    {
                //        if (descriptor == null)
                //        {
                //            cf.loadTarget(mv);
                //        }
                //        if (descriptor == null || !classDesc.equals(descriptor.substring(1)))
                //        {
                //            mv.visitTypeInsn(CHECKCAST, classDesc);
                //        }
                //    }
                //    else
                //    {
                //        if (descriptor != null)
                //        {
                //            // A static field/method call will not consume what is on the stack,
                //            // it needs to be popped off.
                //            mv.visitInsn(POP);
                //        }
                //    }

                // if (this.member instanceof Method) {
                //        Method method = (Method)this.member;
                //        bool isInterface = method.getDeclaringClass().isInterface();
                //        int opcode = (isStatic ? INVOKESTATIC : isInterface ? INVOKEINTERFACE : INVOKEVIRTUAL);
                //        mv.visitMethodInsn(opcode, classDesc, method.getName(),
                //                CodeFlow.createSignatureDescriptor(method), isInterface);
                //    }

                // else
                //    {
                //        mv.visitFieldInsn((isStatic ? GETSTATIC : GETFIELD), classDesc, this.member.getName(),
                //                CodeFlow.toJvmDescriptor(((Field)this.member).getType()));
                //    }
            }
        }
    }
}
