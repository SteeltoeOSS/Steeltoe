// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class PropertyOrFieldReference : SpelNode
    {
        private readonly bool _nullSafe;
        private readonly string _name;
        private TypeDescriptor _originalPrimitiveExitTypeDescriptor;
        private volatile IPropertyAccessor _cachedReadAccessor;
        private volatile IPropertyAccessor _cachedWriteAccessor;

        public PropertyOrFieldReference(bool nullSafe, string propertyOrFieldName, int startPos, int endPos)
        : base(startPos, endPos)
        {
            _nullSafe = nullSafe;
            _name = propertyOrFieldName;
        }

        public bool IsNullSafe => _nullSafe;

        public string Name => _name;

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            var tv = GetValueInternal(state.GetActiveContextObject(), state.EvaluationContext, state.Configuration.AutoGrowNullReferences);
            var accessorToUse = _cachedReadAccessor;
            if (accessorToUse is ICompilablePropertyAccessor accessor)
            {
                var descriptor = ComputeExitDescriptor(tv.Value, accessor.GetPropertyType());
                SetExitTypeDescriptor(descriptor);
            }

            return tv;
        }

        public override void SetValue(ExpressionState state, object newValue)
        {
            WriteProperty(state.GetActiveContextObject(), state.EvaluationContext, _name, newValue);
        }

        public override bool IsWritable(ExpressionState state)
        {
            return IsWritableProperty(_name, state.GetActiveContextObject(), state.EvaluationContext);
        }

        public override string ToStringAST()
        {
            return _name;
        }

        public bool IsWritableProperty(string name, ITypedValue contextObject, IEvaluationContext evalContext)
        {
            var value = contextObject.Value;
            if (value != null)
            {
                var accessorsToTry = GetPropertyAccessorsToTry(contextObject.Value, evalContext.PropertyAccessors);
                foreach (var accessor in accessorsToTry)
                {
                    try
                    {
                        if (accessor.CanWrite(evalContext, value, name))
                        {
                            return true;
                        }
                    }
                    catch (AccessException)
                    {
                        // let others try
                    }
                }
            }

            return false;
        }

        public override bool IsCompilable()
        {
            var accessorToUse = _cachedReadAccessor;
            return accessorToUse is ICompilablePropertyAccessor accessor && accessor.IsCompilable();
        }

        public override void GenerateCode(ILGenerator gen, CodeFlow cf)
        {
            if (_cachedReadAccessor is not ICompilablePropertyAccessor accessorToUse)
            {
                throw new InvalidOperationException("Property accessor is not compilable: " + _cachedReadAccessor);
            }

            Label? skipIfNullLabel = null;
            if (_nullSafe)
            {
                skipIfNullLabel = gen.DefineLabel();
                gen.Emit(OpCodes.Dup);
                gen.Emit(OpCodes.Ldnull);
                gen.Emit(OpCodes.Cgt_Un);
                gen.Emit(OpCodes.Brfalse, skipIfNullLabel.Value);
            }

            accessorToUse.GenerateCode(_name, gen, cf);
            cf.PushDescriptor(_exitTypeDescriptor);
            if (_originalPrimitiveExitTypeDescriptor != null)
            {
                // The output of the accessor is a primitive but from the block above it might be null,
                // so to have a common stack element type at skipIfNull target it is necessary
                // to box the primitive
                CodeFlow.InsertBoxIfNecessary(gen, _originalPrimitiveExitTypeDescriptor);
            }

            if (skipIfNullLabel.HasValue)
            {
                gen.MarkLabel(skipIfNullLabel.Value);
            }
        }

        protected internal override IValueRef GetValueRef(ExpressionState state)
        {
            return new AccessorLValue(this, state.GetActiveContextObject(), state.EvaluationContext, state.Configuration.AutoGrowNullReferences);
        }

        protected internal TypeDescriptor ComputeExitDescriptor(object result, Type propertyReturnType)
        {
            if (propertyReturnType.IsValueType)
            {
                return CodeFlow.ToDescriptor(propertyReturnType);
            }

            return CodeFlow.ToDescriptorFromObject(result);
        }

        protected internal void SetExitTypeDescriptor(TypeDescriptor descriptor)
        {
            // If this property or field access would return a primitive - and yet
            // it is also marked null safe - then the exit type descriptor must be
            // promoted to the box type to allow a null value to be passed on
            if (_nullSafe && CodeFlow.IsValueType(descriptor))
            {
                _originalPrimitiveExitTypeDescriptor = descriptor;
                _exitTypeDescriptor = CodeFlow.ToBoxedDescriptor(descriptor);
            }
            else
            {
                _exitTypeDescriptor = descriptor;
            }
        }

        private ITypedValue GetValueInternal(ITypedValue contextObject, IEvaluationContext evalContext, bool isAutoGrowNullReferences)
        {
            var result = ReadProperty(contextObject, evalContext, _name);

            // Dynamically create the objects if the user has requested that optional behavior
            if (result.Value == null && isAutoGrowNullReferences && NextChildIs(typeof(Indexer), typeof(PropertyOrFieldReference)))
            {
                var resultDescriptor = ClassUtils.GetGenericTypeDefinition(result.TypeDescriptor);
                if (resultDescriptor == null)
                {
                    throw new InvalidOperationException("No result type");
                }

                // Create a new collection or map ready for the indexer
                if (typeof(List<>) == resultDescriptor || typeof(IList<>) == resultDescriptor)
                {
                    if (IsWritableProperty(_name, contextObject, evalContext))
                    {
                        var newList = Activator.CreateInstance(typeof(List<>).MakeGenericType(result.TypeDescriptor.GetGenericArguments()));

                        WriteProperty(contextObject, evalContext, _name, newList);
                        result = ReadProperty(contextObject, evalContext, _name);
                    }
                }
                else if (typeof(Dictionary<,>) == resultDescriptor || typeof(IDictionary<,>) == resultDescriptor)
                {
                    if (IsWritableProperty(_name, contextObject, evalContext))
                    {
                        var newMap = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(result.TypeDescriptor.GetGenericArguments()));

                        WriteProperty(contextObject, evalContext, _name, newMap);
                        result = ReadProperty(contextObject, evalContext, _name);
                    }
                }
                else if (typeof(IDictionary) == resultDescriptor)
                {
                    if (IsWritableProperty(_name, contextObject, evalContext))
                    {
                        var newMap = new Dictionary<object, object>();
                        WriteProperty(contextObject, evalContext, _name, newMap);
                        result = ReadProperty(contextObject, evalContext, _name);
                    }
                }
                else if (typeof(IList) == resultDescriptor)
                {
                    if (IsWritableProperty(_name, contextObject, evalContext))
                    {
                        var newList = new ArrayList();
                        WriteProperty(contextObject, evalContext, _name, newList);
                        result = ReadProperty(contextObject, evalContext, _name);
                    }
                }
                else
                {
                    // 'simple' object
                    try
                    {
                        if (IsWritableProperty(_name, contextObject, evalContext))
                        {
                            var clazz = result.TypeDescriptor;
                            var newObject = ReflectionHelper.GetAccessibleConstructor(clazz).Invoke(Array.Empty<object>());
                            WriteProperty(contextObject, evalContext, _name, newObject);
                            result = ReadProperty(contextObject, evalContext, _name);
                        }
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw new SpelEvaluationException(StartPosition, ex.InnerException, SpelMessage.UNABLE_TO_DYNAMICALLY_CREATE_OBJECT, result.TypeDescriptor);
                    }
                    catch (Exception ex)
                    {
                        throw new SpelEvaluationException(StartPosition, ex, SpelMessage.UNABLE_TO_DYNAMICALLY_CREATE_OBJECT, result.TypeDescriptor);
                    }
                }
            }

            return result;
        }

        private ITypedValue ReadProperty(ITypedValue contextObject, IEvaluationContext evalContext, string name)
        {
            var targetObject = contextObject.Value;
            if (targetObject == null && _nullSafe)
            {
                return TypedValue.NULL;
            }

            var accessorToUse = _cachedReadAccessor;
            if (accessorToUse != null)
            {
                if (accessorToUse is ReflectivePropertyAccessor.OptimalPropertyAccessor || evalContext.PropertyAccessors.Contains(accessorToUse))
                {
                    try
                    {
                        return accessorToUse.Read(evalContext, contextObject.Value, name);
                    }
                    catch (Exception)
                    {
                        // This is OK - it may have gone stale due to a class change,
                        // let's try to get a new one and call it before giving up...
                    }
                }

                _cachedReadAccessor = null;
            }

            var accessorsToTry = GetPropertyAccessorsToTry(contextObject.Value, evalContext.PropertyAccessors);

            // Go through the accessors that may be able to resolve it. If they are a cacheable accessor then
            // get the accessor and use it. If they are not cacheable but report they can read the property
            // then ask them to read it
            try
            {
                foreach (var acc in accessorsToTry)
                {
                    var accessor = acc;
                    if (accessor.CanRead(evalContext, contextObject.Value, name))
                    {
                        if (accessor is ReflectivePropertyAccessor accessor1)
                        {
                            accessor = accessor1.CreateOptimalAccessor(evalContext, contextObject.Value, name);
                        }

                        _cachedReadAccessor = accessor;
                        return accessor.Read(evalContext, contextObject.Value, name);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new SpelEvaluationException(ex, SpelMessage.EXCEPTION_DURING_PROPERTY_READ, name, ex.Message);
            }

            if (contextObject.Value == null)
            {
                throw new SpelEvaluationException(SpelMessage.PROPERTY_OR_FIELD_NOT_READABLE_ON_NULL, name);
            }
            else
            {
                throw new SpelEvaluationException(StartPosition, SpelMessage.PROPERTY_OR_FIELD_NOT_READABLE, _name, FormatHelper.FormatClassNameForMessage(GetObjectType(contextObject.Value)));
            }
        }

        private void WriteProperty(ITypedValue contextObject, IEvaluationContext evalContext, string name, object newValue)
        {
            if (contextObject.Value == null && _nullSafe)
            {
                return;
            }

            if (contextObject.Value == null)
            {
                throw new SpelEvaluationException(StartPosition, SpelMessage.PROPERTY_OR_FIELD_NOT_WRITABLE_ON_NULL, name);
            }

            var accessorToUse = _cachedWriteAccessor;
            if (accessorToUse != null)
            {
                if (accessorToUse is ReflectivePropertyAccessor.OptimalPropertyAccessor || evalContext.PropertyAccessors.Contains(accessorToUse))
                {
                    try
                    {
                        accessorToUse.Write(evalContext, contextObject.Value, name, newValue);
                        return;
                    }
                    catch (Exception)
                    {
                        // This is OK - it may have gone stale due to a class change,
                        // let's try to get a new one and call it before giving up...
                    }
                }

                _cachedWriteAccessor = null;
            }

            var accessorsToTry = GetPropertyAccessorsToTry(contextObject.Value, evalContext.PropertyAccessors);
            try
            {
                foreach (var accessor in accessorsToTry)
                {
                    if (accessor.CanWrite(evalContext, contextObject.Value, name))
                    {
                        _cachedWriteAccessor = accessor;
                        accessor.Write(evalContext, contextObject.Value, name, newValue);
                        return;
                    }
                }
            }
            catch (AccessException ex)
            {
                throw new SpelEvaluationException(StartPosition, ex, SpelMessage.EXCEPTION_DURING_PROPERTY_WRITE, name, ex.Message);
            }

            throw new SpelEvaluationException(StartPosition, SpelMessage.PROPERTY_OR_FIELD_NOT_WRITABLE, name, FormatHelper.FormatClassNameForMessage(GetObjectType(contextObject.Value)));
        }

        private IList<IPropertyAccessor> GetPropertyAccessorsToTry(object contextObject, IList<IPropertyAccessor> propertyAccessors)
        {
            var targetType = contextObject?.GetType();

            var specificAccessors = new List<IPropertyAccessor>();
            var generalAccessors = new List<IPropertyAccessor>();
            foreach (var resolver in propertyAccessors)
            {
                var targets = resolver.GetSpecificTargetClasses();
                if (targets == null)
                {
                    // generic resolver that says it can be used for any type
                    generalAccessors.Add(resolver);
                }
                else if (targetType != null)
                {
                    foreach (var clazz in targets)
                    {
                        if (clazz == targetType)
                        {
                            specificAccessors.Add(resolver);
                            break;
                        }
                        else if (clazz.IsAssignableFrom(targetType))
                        {
                            generalAccessors.Add(resolver);
                        }
                    }
                }
            }

            var resolvers = new List<IPropertyAccessor>(specificAccessors);
            generalAccessors.RemoveAll((a) => specificAccessors.Contains(a));
            resolvers.AddRange(generalAccessors);
            return resolvers;
        }

        private class AccessorLValue : IValueRef
        {
            private readonly PropertyOrFieldReference _ref;

            private readonly ITypedValue _contextObject;

            private readonly IEvaluationContext _evalContext;

            private readonly bool _autoGrowNullReferences;

            public AccessorLValue(PropertyOrFieldReference propertyOrFieldReference, ITypedValue activeContextObject, IEvaluationContext evalContext, bool autoGrowNullReferences)
            {
                _ref = propertyOrFieldReference;
                _contextObject = activeContextObject;
                _evalContext = evalContext;
                _autoGrowNullReferences = autoGrowNullReferences;
            }

            public ITypedValue GetValue()
            {
                var value = _ref.GetValueInternal(_contextObject, _evalContext, _autoGrowNullReferences);
                var accessorToUse = _ref._cachedReadAccessor;
                if (accessorToUse is ICompilablePropertyAccessor accessor)
                {
                    var descriptor = _ref.ComputeExitDescriptor(value.Value, accessor.GetPropertyType());
                    _ref.SetExitTypeDescriptor(descriptor);
                }

                return value;
            }

            public void SetValue(object newValue)
            {
                _ref.WriteProperty(_contextObject, _evalContext, _ref._name, newValue);
            }

            public bool IsWritable
            {
                get
                {
                    return _ref.IsWritableProperty(_ref._name, _contextObject, _evalContext);
                }
            }
        }
    }
}
