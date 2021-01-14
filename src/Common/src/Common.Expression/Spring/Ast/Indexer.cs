﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class Indexer : SpelNode
    {
        private enum IndexedType
        {
            ARRAY,
            LIST,
            MAP,
            STRING,
            OBJECT
        }

        // These fields are used when the indexer is being used as a property read accessor.
        // If the name and target type match these cached values then the cachedReadAccessor
        // is used to read the property. If they do not match, the correct accessor is
        // discovered and then cached for later use.
        private string _cachedReadName;

        private Type _cachedReadTargetType;

        private IPropertyAccessor _cachedReadAccessor;

        // These fields are used when the indexer is being used as a property write accessor.
        // If the name and target type match these cached values then the cachedWriteAccessor
        // is used to write the property. If they do not match, the correct accessor is
        // discovered and then cached for later use.
        private string _cachedWriteName;

        private Type _cachedWriteTargetType;

        private IPropertyAccessor _cachedWriteAccessor;

        private IndexedType _indexedType;

        public Indexer(int startPos, int endPos, SpelNode expr)
            : base(startPos, endPos, expr)
        {
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            return GetValueRef(state).GetValue();
        }

        public override void SetValue(ExpressionState state, object newValue)
        {
            GetValueRef(state).SetValue(newValue);
        }

        public override bool IsWritable(ExpressionState expressionState)
        {
            return true;
        }

        public override bool IsCompilable()
        {
            if (_indexedType == IndexedType.ARRAY)
            {
                return _exitTypeDescriptor != null;
            }
            else if (_indexedType == IndexedType.LIST)
            {
                return _children[0].IsCompilable();
            }
            else if (_indexedType == IndexedType.MAP)
            {
                return _children[0] is PropertyOrFieldReference || _children[0].IsCompilable();
            }
            else if (_indexedType == IndexedType.OBJECT)
            {
                // If the string name is changing the accessor is clearly going to change (so no compilation possible)
                return (_cachedReadAccessor is ReflectivePropertyAccessor.OptimalPropertyAccessor) && (GetChild(0) is StringLiteral);
            }

            return false;
        }

        public override void GenerateCode(DynamicMethod mv, CodeFlow cf)
        {
            // String descriptor = cf.lastDescriptor();
            // if (descriptor == null)
            // {
            //    // Stack is empty, should use context object
            //    cf.loadTarget(mv);
            // }

            // if (this.indexedType == IndexedType.ARRAY)
            // {
            //    int insn;
            //    if ("D".Equals(this.exitTypeDescriptor))
            //    {
            //        mv.visitTypeInsn(CHECKCAST, "[D");
            //        insn = DALOAD;
            //    }
            //    else if ("F".Equals(this.exitTypeDescriptor))
            //    {
            //        mv.visitTypeInsn(CHECKCAST, "[F");
            //        insn = FALOAD;
            //    }
            //    else if ("J".Equals(this.exitTypeDescriptor))
            //    {
            //        mv.visitTypeInsn(CHECKCAST, "[J");
            //        insn = LALOAD;
            //    }
            //    else if ("I".Equals(this.exitTypeDescriptor))
            //    {
            //        mv.visitTypeInsn(CHECKCAST, "[I");
            //        insn = IALOAD;
            //    }
            //    else if ("S".Equals(this.exitTypeDescriptor))
            //    {
            //        mv.visitTypeInsn(CHECKCAST, "[S");
            //        insn = SALOAD;
            //    }
            //    else if ("B".Equals(this.exitTypeDescriptor))
            //    {
            //        mv.visitTypeInsn(CHECKCAST, "[B");
            //        insn = BALOAD;
            //    }
            //    else if ("C".Equals(this.exitTypeDescriptor))
            //    {
            //        mv.visitTypeInsn(CHECKCAST, "[C");
            //        insn = CALOAD;
            //    }
            //    else
            //    {
            //        mv.visitTypeInsn(CHECKCAST, "[" + this.exitTypeDescriptor +
            //                (CodeFlow.isPrimitiveArray(this.exitTypeDescriptor) ? "" : ";"));
            //        //depthPlusOne(exitTypeDescriptor)+"Ljava/lang/Object;");
            //        insn = AALOAD;
            //    }
            //    SpelNodeImpl index = this.children[0];
            //    cf.enterCompilationScope();
            //    index.generateCode(mv, cf);
            //    cf.exitCompilationScope();
            //    mv.visitInsn(insn);
            // }

            // else if (this.indexedType == IndexedType.LIST)
            // {
            //    mv.visitTypeInsn(CHECKCAST, "java/util/List");
            //    cf.enterCompilationScope();
            //    this.children[0].generateCode(mv, cf);
            //    cf.exitCompilationScope();
            //    mv.visitMethodInsn(INVOKEINTERFACE, "java/util/List", "get", "(I)Ljava/lang/Object;", true);
            // }

            // else if (this.indexedType == IndexedType.MAP)
            // {
            //    mv.visitTypeInsn(CHECKCAST, "java/util/Map");
            //    // Special case when the key is an unquoted string literal that will be parsed as
            //    // a property/field reference
            //    if ((this.children[0] instanceof PropertyOrFieldReference)) {
            //        PropertyOrFieldReference reference = (PropertyOrFieldReference)this.children[0];
            //        String mapKeyName = reference.getName();
            //        mv.visitLdcInsn(mapKeyName);
            //    }

            // else
            //    {
            //        cf.enterCompilationScope();
            //        this.children[0].generateCode(mv, cf);
            //        cf.exitCompilationScope();
            //    }
            //    mv.visitMethodInsn(
            //            INVOKEINTERFACE, "java/util/Map", "get", "(Ljava/lang/Object;)Ljava/lang/Object;", true);
            // }

            // else if (this.indexedType == IndexedType.OBJECT)
            // {
            //    ReflectivePropertyAccessor.OptimalPropertyAccessor accessor =
            //            (ReflectivePropertyAccessor.OptimalPropertyAccessor)this.cachedReadAccessor;
            //    Assert.state(accessor != null, "No cached read accessor");
            //    Member member = accessor.member;
            //    boolean isStatic = Modifier.isStatic(member.getModifiers());
            //    String classDesc = member.getDeclaringClass().getName().replace('.', '/');

            // if (!isStatic)
            //    {
            //        if (descriptor == null)
            //        {
            //            cf.loadTarget(mv);
            //        }
            //        if (descriptor == null || !classDesc.Equals(descriptor.substring(1)))
            //        {
            //            mv.visitTypeInsn(CHECKCAST, classDesc);
            //        }
            //    }

            // if (member instanceof Method) {
            //        mv.visitMethodInsn((isStatic ? INVOKESTATIC : INVOKEVIRTUAL), classDesc, member.getName(),
            //                CodeFlow.createSignatureDescriptor((Method)member), false);
            //    }

            // else
            //    {
            //        mv.visitFieldInsn((isStatic ? GETSTATIC : GETFIELD), classDesc, member.getName(),
            //                CodeFlow.toJvmDescriptor(((Field)member).getType()));
            //    }
            // }

            // cf.pushDescriptor(this.exitTypeDescriptor);
        }

        public override string ToStringAST()
        {
            var sj = new List<string>();
            for (var i = 0; i < ChildCount; i++)
            {
                sj.Add(GetChild(i).ToStringAST());
            }

            return "[" + string.Join(",", sj) + "]";
        }

        protected internal override IValueRef GetValueRef(ExpressionState state)
        {
            var context = state.GetActiveContextObject();
            var target = context.Value;
            var targetDescriptor = context.TypeDescriptor;
            ITypedValue indexValue;
            object index;

            // This first part of the if clause prevents a 'double dereference' of the property (SPR-5847)
            if (target is System.Collections.IDictionary && (_children[0] is PropertyOrFieldReference))
            {
                var reference = (PropertyOrFieldReference)_children[0];
                index = reference.Name;
                indexValue = new TypedValue(index);
            }
            else
            {
                // In case the map key is unqualified, we want it evaluated against the root object
                // so temporarily push that on whilst evaluating the key
                try
                {
                    state.PushActiveContextObject(state.RootContextObject);
                    indexValue = _children[0].GetValueInternal(state);
                    index = indexValue.Value;
                    if (index == null)
                    {
                        throw new InvalidOperationException("No index");
                    }
                }
                finally
                {
                    state.PopActiveContextObject();
                }
            }

            // Raise a proper exception in case of a null target
            if (target == null)
            {
                throw new SpelEvaluationException(StartPosition, SpelMessage.CANNOT_INDEX_INTO_NULL_VALUE);
            }

            // At this point, we need a TypeDescriptor for a non-null target object
            if (targetDescriptor == null)
            {
                throw new InvalidOperationException("No type descriptor");
            }

            // Indexing into a Map
            if (target is System.Collections.IDictionary)
            {
                var key = index;
                var mapkeyType = ReflectionHelper.GetMapKeyTypeDescriptor(targetDescriptor);
                if (mapkeyType != null)
                {
                    key = state.ConvertValue(key, mapkeyType);
                }

                _indexedType = IndexedType.MAP;
                return new MapIndexingValueRef(this, state.TypeConverter, (IDictionary)target, key, targetDescriptor);
            }

            // If the object is something that looks indexable by an integer,
            // attempt to treat the index value as a number
            if (target is Array || target is IList || target is string)
            {
                var idx = (int)state.ConvertValue(index, typeof(int));
                if (target is Array)
                {
                    _indexedType = IndexedType.ARRAY;
                    return new ArrayIndexingValueRef(this, state.TypeConverter, target, idx, targetDescriptor);
                }
                else if (target is IList)
                {
                    _indexedType = IndexedType.LIST;

                    return new CollectionIndexingValueRef(this, (IList)target, idx, targetDescriptor, state.TypeConverter, state.Configuration.AutoGrowCollections, state.Configuration.MaximumAutoGrowSize);
                }
                else
                {
                    _indexedType = IndexedType.STRING;
                    return new StringIndexingLValue(this, (string)target, idx, targetDescriptor);
                }
            }

            // Try and treat the index value as a property of the context object
            // TODO: could call the conversion service to convert the value to a String
            var valueType = indexValue.TypeDescriptor;
            if (valueType != null && typeof(string) == valueType)
            {
                _indexedType = IndexedType.OBJECT;
                return new PropertyIndexingValueRef(this, target, (string)index, state.EvaluationContext, targetDescriptor);
            }

            throw new SpelEvaluationException(StartPosition, SpelMessage.INDEXING_NOT_SUPPORTED_FOR_TYPE, targetDescriptor);
        }

        private void CheckAccess(int arrayLength, int index)
        {
            if (index >= arrayLength)
            {
                throw new SpelEvaluationException(StartPosition, SpelMessage.ARRAY_INDEX_OUT_OF_BOUNDS, arrayLength, index);
            }
        }

        private T ConvertValue<T>(ITypeConverter converter, object value)
        {
            var targetType = typeof(T);
            var result = (T)converter.ConvertValue(value, value == null ? typeof(object) : value.GetType(), targetType);
            if (result == null)
            {
                throw new InvalidOperationException("Null conversion result for index [" + value + "]");
            }

            return result;
        }

        private object AccessArrayElement(object ctx, int idx)
        {
            var arrayComponentType = ctx.GetType().GetElementType();
            if (arrayComponentType == typeof(bool))
            {
                var array = (bool[])ctx;
                CheckAccess(array.Length, idx);
                _exitTypeDescriptor = "Z";
                return array[idx];
            }
            else if (arrayComponentType == typeof(byte))
            {
                var array = (byte[])ctx;
                CheckAccess(array.Length, idx);

                _exitTypeDescriptor = "B";
                return array[idx];
            }
            else if (arrayComponentType == typeof(char))
            {
                var array = (char[])ctx;
                CheckAccess(array.Length, idx);

                _exitTypeDescriptor = "C";
                return array[idx];
            }
            else if (arrayComponentType == typeof(double))
            {
                var array = (double[])ctx;
                CheckAccess(array.Length, idx);

                _exitTypeDescriptor = "D";
                return array[idx];
            }
            else if (arrayComponentType == typeof(float))
            {
                var array = (float[])ctx;
                CheckAccess(array.Length, idx);

                _exitTypeDescriptor = "F";
                return array[idx];
            }
            else if (arrayComponentType == typeof(int))
            {
                var array = (int[])ctx;
                CheckAccess(array.Length, idx);

                _exitTypeDescriptor = "I";
                return array[idx];
            }
            else if (arrayComponentType == typeof(long))
            {
                var array = (long[])ctx;
                CheckAccess(array.Length, idx);

                _exitTypeDescriptor = "J";
                return array[idx];
            }
            else if (arrayComponentType == typeof(short))
            {
                var array = (short[])ctx;
                CheckAccess(array.Length, idx);

                _exitTypeDescriptor = "S";
                return array[idx];
            }
            else
            {
                var array = (object[])ctx;
                CheckAccess(array.Length, idx);
                var retValue = array[idx];

                _exitTypeDescriptor = CodeFlow.ToDescriptor(arrayComponentType);
                return retValue;
            }
        }

        private void SetArrayElement(ITypeConverter converter, object ctx, int idx, object newValue, Type arrayComponentType)
        {
            if (arrayComponentType == typeof(bool))
            {
                var array = (bool[])ctx;
                CheckAccess(array.Length, idx);
                array[idx] = ConvertValue<bool>(converter, newValue);
            }
            else if (arrayComponentType == typeof(byte))
            {
                var array = (byte[])ctx;
                CheckAccess(array.Length, idx);
                array[idx] = ConvertValue<byte>(converter, newValue);
            }
            else if (arrayComponentType == typeof(char))
            {
                var array = (char[])ctx;
                CheckAccess(array.Length, idx);
                array[idx] = ConvertValue<char>(converter, newValue);
            }
            else if (arrayComponentType == typeof(double))
            {
                var array = (double[])ctx;
                CheckAccess(array.Length, idx);
                array[idx] = ConvertValue<double>(converter, newValue);
            }
            else if (arrayComponentType == typeof(float))
            {
                var array = (float[])ctx;
                CheckAccess(array.Length, idx);
                array[idx] = ConvertValue<float>(converter, newValue);
            }
            else if (arrayComponentType == typeof(int))
            {
                var array = (int[])ctx;
                CheckAccess(array.Length, idx);
                array[idx] = ConvertValue<int>(converter, newValue);
            }
            else if (arrayComponentType == typeof(long))
            {
                var array = (long[])ctx;
                CheckAccess(array.Length, idx);
                array[idx] = ConvertValue<long>(converter, newValue);
            }
            else if (arrayComponentType == typeof(short))
            {
                var array = (short[])ctx;
                CheckAccess(array.Length, idx);
                array[idx] = ConvertValue<short>(converter, newValue);
            }
            else
            {
                var array = (object[])ctx;
                CheckAccess(array.Length, idx);
                array[idx] = ConvertValue<object>(converter, newValue);
            }
        }

        private class ArrayIndexingValueRef : IValueRef
        {
            private readonly ITypeConverter _typeConverter;
            private readonly object _array;
            private readonly int _index;
            private readonly Type _typeDescriptor;
            private readonly Indexer _indexer;

            public ArrayIndexingValueRef(Indexer indexer, ITypeConverter typeConverter, object array, int index, Type typeDescriptor)
            {
                _indexer = indexer;
                _typeConverter = typeConverter;
                _array = array;
                _index = index;
                _typeDescriptor = typeDescriptor;
            }

            public ITypedValue GetValue()
            {
                var arrayElement = _indexer.AccessArrayElement(_array, _index);
                var type = arrayElement == null ? _typeDescriptor : arrayElement.GetType();
                return new TypedValue(arrayElement, type);
            }

            public void SetValue(object newValue)
            {
                var elementType = _typeDescriptor.GetElementType();
                if (elementType == null)
                {
                    throw new InvalidOperationException("No element type");
                }

                _indexer.SetArrayElement(_typeConverter, _array, _index, newValue, elementType);
            }

            public bool IsWritable => true;
        }

        private class MapIndexingValueRef : IValueRef
        {
            private readonly Indexer _indexer;

            private readonly ITypeConverter _typeConverter;

            private readonly IDictionary _map;

            private readonly object _key;

            private readonly Type _mapEntryDescriptor;

            public MapIndexingValueRef(Indexer indexer, ITypeConverter typeConverter, IDictionary map, object key, Type mapEntryDescriptor)
            {
                _indexer = indexer;
                _typeConverter = typeConverter;
                _map = map;
                _key = key;
                _mapEntryDescriptor = mapEntryDescriptor;
            }

            public ITypedValue GetValue()
            {
                var value = _map[_key];
                _indexer._exitTypeDescriptor = CodeFlow.ToDescriptor(typeof(object));
                return new TypedValue(value, ReflectionHelper.GetMapValueTypeDescriptor(_mapEntryDescriptor, value));
            }

            public void SetValue(object newValue)
            {
                var mapValType = ReflectionHelper.GetMapValueTypeDescriptor(_mapEntryDescriptor);
                if (mapValType != null)
                {
                    newValue = _typeConverter.ConvertValue(newValue, newValue == null ? typeof(object) : newValue.GetType(), mapValType);
                }

                _map[_key] = newValue;
            }

            public bool IsWritable => true;
        }

        private class PropertyIndexingValueRef : IValueRef
        {
            private readonly object _targetObject;

            private readonly string _name;

            private readonly IEvaluationContext _evaluationContext;

            private readonly Type _targetObjectTypeDescriptor;
            private readonly Indexer _indexer;

            public PropertyIndexingValueRef(Indexer indexer, object targetObject, string value, IEvaluationContext evaluationContext, Type targetObjectTypeDescriptor)
            {
                _indexer = indexer;
                _targetObject = targetObject;
                _name = value;
                _evaluationContext = evaluationContext;
                _targetObjectTypeDescriptor = targetObjectTypeDescriptor;
            }

            public ITypedValue GetValue()
            {
                var targetObjectRuntimeClass = _indexer.GetObjectType(_targetObject);
                try
                {
                    if (_indexer._cachedReadName != null && _indexer._cachedReadName.Equals(_name) && _indexer._cachedReadTargetType != null && _indexer._cachedReadTargetType.Equals(targetObjectRuntimeClass))
                    {
                        // It is OK to use the cached accessor
                        var accessor = _indexer._cachedReadAccessor;
                        if (accessor == null)
                        {
                            throw new InvalidOperationException("No cached read accessor");
                        }

                        return accessor.Read(_evaluationContext, _targetObject, _name);
                    }

                    var accessorsToTry = AstUtils.GetPropertyAccessorsToTry(targetObjectRuntimeClass, _evaluationContext.PropertyAccessors);
                    foreach (var acc in accessorsToTry)
                    {
                        var accessor = acc;
                        if (accessor.CanRead(_evaluationContext, _targetObject, _name))
                        {
                            if (accessor is ReflectivePropertyAccessor)
                            {
                                accessor = ((ReflectivePropertyAccessor)accessor).CreateOptimalAccessor(_evaluationContext, _targetObject, _name);
                            }

                            _indexer._cachedReadAccessor = accessor;
                            _indexer._cachedReadName = _name;
                            _indexer._cachedReadTargetType = targetObjectRuntimeClass;
                            if (accessor is ReflectivePropertyAccessor.OptimalPropertyAccessor)
                            {
                                var optimalAccessor = (ReflectivePropertyAccessor.OptimalPropertyAccessor)accessor;
                                var member = optimalAccessor.Member;
                                _indexer._exitTypeDescriptor = CodeFlow.ToDescriptor(member is MethodInfo ? ((MethodInfo)member).ReturnType : ((FieldInfo)member).FieldType);
                            }

                            return accessor.Read(_evaluationContext, _targetObject, _name);
                        }
                    }
                }
                catch (AccessException ex)
                {
                    throw new SpelEvaluationException(_indexer.StartPosition, ex, SpelMessage.INDEXING_NOT_SUPPORTED_FOR_TYPE, _targetObjectTypeDescriptor.ToString());
                }

                throw new SpelEvaluationException(_indexer.StartPosition, SpelMessage.INDEXING_NOT_SUPPORTED_FOR_TYPE, _targetObjectTypeDescriptor.ToString());
            }

            public void SetValue(object newValue)
            {
                var contextObjectClass = _indexer.GetObjectType(_targetObject);
                try
                {
                    if (_indexer._cachedWriteName != null && _indexer._cachedWriteName.Equals(_name) && _indexer._cachedWriteTargetType != null && _indexer._cachedWriteTargetType.Equals(contextObjectClass))
                    {
                        // It is OK to use the cached accessor
                        var accessor = _indexer._cachedWriteAccessor;
                        if (accessor == null)
                        {
                            throw new InvalidOperationException("No cached write accessor");
                        }

                        accessor.Write(_evaluationContext, _targetObject, _name, newValue);
                        return;
                    }

                    var accessorsToTry = AstUtils.GetPropertyAccessorsToTry(contextObjectClass, _evaluationContext.PropertyAccessors);
                    foreach (var acc in accessorsToTry)
                    {
                        var accessor = acc;
                        if (accessor.CanWrite(_evaluationContext, _targetObject, _name))
                        {
                            _indexer._cachedWriteName = _name;
                            _indexer._cachedWriteTargetType = contextObjectClass;
                            _indexer._cachedWriteAccessor = accessor;
                            accessor.Write(_evaluationContext, _targetObject, _name, newValue);
                            return;
                        }
                    }
                }
                catch (AccessException ex)
                {
                    throw new SpelEvaluationException(_indexer.StartPosition, ex, SpelMessage.EXCEPTION_DURING_PROPERTY_WRITE, _name, ex.Message);
                }
            }

            public bool IsWritable => true;
        }

        private class CollectionIndexingValueRef : IValueRef
        {
            private readonly IList _collection;
            private readonly int _index;
            private readonly Type _collectionEntryDescriptor;
            private readonly ITypeConverter _typeConverter;
            private readonly bool _growCollection;
            private readonly int _maximumSize;
            private readonly Indexer _indexer;

            public CollectionIndexingValueRef(Indexer indexer, IList collection, int index, Type collectionEntryDescriptor, ITypeConverter typeConverter, bool growCollection, int maximumSize)
            {
                _indexer = indexer;
                _collection = collection;
                _index = index;
                _collectionEntryDescriptor = collectionEntryDescriptor;
                _typeConverter = typeConverter;
                _growCollection = growCollection;
                _maximumSize = maximumSize;
            }

            public ITypedValue GetValue()
            {
                GrowCollectionIfNecessary();
                if (_collection is IList)
                {
                    var o = ((IList)_collection)[_index];
                    _indexer._exitTypeDescriptor = CodeFlow.ToDescriptor(typeof(object));
                    return new TypedValue(o, ReflectionHelper.GetElementTypeDescriptor(_collectionEntryDescriptor, o));
                }

                var pos = 0;
                foreach (var o in _collection)
                {
                    if (pos == _index)
                    {
                        return new TypedValue(o, ReflectionHelper.GetElementTypeDescriptor(_collectionEntryDescriptor, o));
                    }

                    pos++;
                }

                throw new InvalidOperationException("Failed to find indexed element " + _index + ": " + _collection);
            }

            public void SetValue(object newValue)
            {
                GrowCollectionIfNecessary();
                if (_collection is IList)
                {
                    var list = (IList)_collection;
                    var elemTypeDesc = ReflectionHelper.GetElementTypeDescriptor(_collectionEntryDescriptor);
                    if (elemTypeDesc != null)
                    {
                        newValue = _typeConverter.ConvertValue(newValue, newValue == null ? typeof(object) : newValue.GetType(), elemTypeDesc);
                    }

                    list[_index] = newValue;
                }
                else
                {
                    throw new SpelEvaluationException(_indexer.StartPosition, SpelMessage.INDEXING_NOT_SUPPORTED_FOR_TYPE, _collectionEntryDescriptor.ToString());
                }
            }

            private void GrowCollectionIfNecessary()
            {
                if (_index >= _collection.Count)
                {
                    if (!_growCollection)
                    {
                        throw new SpelEvaluationException(_indexer.StartPosition, SpelMessage.COLLECTION_INDEX_OUT_OF_BOUNDS, _collection.Count, _index);
                    }

                    if (_index >= _maximumSize)
                    {
                        throw new SpelEvaluationException(_indexer.StartPosition, SpelMessage.UNABLE_TO_GROW_COLLECTION);
                    }

                    var elemTypeDesc = ReflectionHelper.GetElementTypeDescriptor(_collectionEntryDescriptor);
                    if (elemTypeDesc == null)
                    {
                        throw new SpelEvaluationException(_indexer.StartPosition, SpelMessage.UNABLE_TO_GROW_COLLECTION_UNKNOWN_ELEMENT_TYPE);
                    }

                    try
                    {
                        // var ctor = GetDefaultConstructor(elemTypeDesc);
                        var newElements = _index - _collection.Count;
                        while (newElements >= 0)
                        {
                            // Insert a null value if the element type does not have a default constructor.
                            // _collection.Add(ctor != null ? ctor.Invoke(new object[0]) : null);
                            _collection.Add(GetDefaultValue(elemTypeDesc));
                            newElements--;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new SpelEvaluationException(_indexer.StartPosition, ex, SpelMessage.UNABLE_TO_GROW_COLLECTION);
                    }
                }
            }

            private object GetDefaultValue(Type elemTypeDesc)
            {
                if (elemTypeDesc == typeof(string))
                {
                    return string.Empty;
                }

                if (elemTypeDesc == typeof(int))
                {
                    return 0;
                }

                if (elemTypeDesc == typeof(short))
                {
                    return (short)0;
                }

                if (elemTypeDesc == typeof(long))
                {
                    return 0L;
                }

                if (elemTypeDesc == typeof(uint))
                {
                    return 0U;
                }

                if (elemTypeDesc == typeof(ushort))
                {
                    return (ushort)0;
                }

                if (elemTypeDesc == typeof(ulong))
                {
                    return 0UL;
                }

                if (elemTypeDesc == typeof(byte))
                {
                    return (byte)0;
                }

                if (elemTypeDesc == typeof(sbyte))
                {
                    return (sbyte)0;
                }

                if (elemTypeDesc == typeof(char))
                {
                    return (char)0;
                }

                return Activator.CreateInstance(elemTypeDesc);
            }

            // private ConstructorInfo GetDefaultConstructor(Type type)
            // {
            //    try
            //    {
            //        return type.GetConstructor(new Type[0]);
            //    }
            //    catch (Exception)
            //    {
            //        return null;
            //    }
            // }
            public bool IsWritable => true;
        }

        private class StringIndexingLValue : IValueRef
        {
            private readonly string _target;

            private readonly int _index;

            private readonly Type _typeDescriptor;
            private readonly Indexer _indexer;

            public StringIndexingLValue(Indexer indexer, string target, int index, Type typeDescriptor)
            {
                _indexer = indexer;
                _target = target;
                _index = index;
                _typeDescriptor = typeDescriptor;
            }

            public ITypedValue GetValue()
            {
                if (_index >= _target.Length)
                {
                    throw new SpelEvaluationException(_indexer.StartPosition, SpelMessage.STRING_INDEX_OUT_OF_BOUNDS, _target.Length, _index);
                }

                return new TypedValue(_target[_index].ToString());
            }

            public void SetValue(object newValue)
            {
                throw new SpelEvaluationException(_indexer.StartPosition, SpelMessage.INDEXING_NOT_SUPPORTED_FOR_TYPE, _typeDescriptor.ToString());
            }

            public bool IsWritable => true;
        }
    }
}
