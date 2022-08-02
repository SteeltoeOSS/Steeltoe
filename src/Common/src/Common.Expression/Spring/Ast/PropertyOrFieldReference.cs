// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using Steeltoe.Common.Util;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class PropertyOrFieldReference : SpelNode
{
    private TypeDescriptor _originalPrimitiveExitTypeDescriptor;
    private volatile IPropertyAccessor _cachedReadAccessor;
    private volatile IPropertyAccessor _cachedWriteAccessor;

    public bool IsNullSafe { get; }

    public string Name { get; }

    public PropertyOrFieldReference(bool nullSafe, string propertyOrFieldName, int startPos, int endPos)
        : base(startPos, endPos)
    {
        IsNullSafe = nullSafe;
        Name = propertyOrFieldName;
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        ITypedValue tv = GetValueInternal(state.GetActiveContextObject(), state.EvaluationContext, state.Configuration.AutoGrowNullReferences);
        IPropertyAccessor accessorToUse = _cachedReadAccessor;

        if (accessorToUse is ICompilablePropertyAccessor accessor)
        {
            TypeDescriptor descriptor = ComputeExitDescriptor(tv.Value, accessor.GetPropertyType());
            SetExitTypeDescriptor(descriptor);
        }

        return tv;
    }

    public override void SetValue(ExpressionState state, object newValue)
    {
        WriteProperty(state.GetActiveContextObject(), state.EvaluationContext, Name, newValue);
    }

    public override bool IsWritable(ExpressionState state)
    {
        return IsWritableProperty(Name, state.GetActiveContextObject(), state.EvaluationContext);
    }

    public override string ToStringAst()
    {
        return Name;
    }

    public bool IsWritableProperty(string name, ITypedValue contextObject, IEvaluationContext evalContext)
    {
        object value = contextObject.Value;

        if (value != null)
        {
            IList<IPropertyAccessor> accessorsToTry = GetPropertyAccessorsToTry(contextObject.Value, evalContext.PropertyAccessors);

            foreach (IPropertyAccessor accessor in accessorsToTry)
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
        IPropertyAccessor accessorToUse = _cachedReadAccessor;
        return accessorToUse is ICompilablePropertyAccessor accessor && accessor.IsCompilable();
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        if (_cachedReadAccessor is not ICompilablePropertyAccessor accessorToUse)
        {
            throw new InvalidOperationException($"Property accessor is not compilable: {_cachedReadAccessor}");
        }

        Label? skipIfNullLabel = null;

        if (IsNullSafe)
        {
            skipIfNullLabel = gen.DefineLabel();
            gen.Emit(OpCodes.Dup);
            gen.Emit(OpCodes.Ldnull);
            gen.Emit(OpCodes.Cgt_Un);
            gen.Emit(OpCodes.Brfalse, skipIfNullLabel.Value);
        }

        accessorToUse.GenerateCode(Name, gen, cf);
        cf.PushDescriptor(exitTypeDescriptor);

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
        if (IsNullSafe && CodeFlow.IsValueType(descriptor))
        {
            _originalPrimitiveExitTypeDescriptor = descriptor;
            exitTypeDescriptor = CodeFlow.ToBoxedDescriptor(descriptor);
        }
        else
        {
            exitTypeDescriptor = descriptor;
        }
    }

    private ITypedValue GetValueInternal(ITypedValue contextObject, IEvaluationContext evalContext, bool isAutoGrowNullReferences)
    {
        ITypedValue result = ReadProperty(contextObject, evalContext, Name);

        // Dynamically create the objects if the user has requested that optional behavior
        if (result.Value == null && isAutoGrowNullReferences && NextChildIs(typeof(Indexer), typeof(PropertyOrFieldReference)))
        {
            Type resultDescriptor = ClassUtils.GetGenericTypeDefinition(result.TypeDescriptor);

            if (resultDescriptor == null)
            {
                throw new InvalidOperationException("No result type");
            }

            // Create a new collection or map ready for the indexer
            if (typeof(List<>) == resultDescriptor || typeof(IList<>) == resultDescriptor)
            {
                if (IsWritableProperty(Name, contextObject, evalContext))
                {
                    object newList = Activator.CreateInstance(typeof(List<>).MakeGenericType(result.TypeDescriptor.GetGenericArguments()));

                    WriteProperty(contextObject, evalContext, Name, newList);
                    result = ReadProperty(contextObject, evalContext, Name);
                }
            }
            else if (typeof(Dictionary<,>) == resultDescriptor || typeof(IDictionary<,>) == resultDescriptor)
            {
                if (IsWritableProperty(Name, contextObject, evalContext))
                {
                    object newMap = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(result.TypeDescriptor.GetGenericArguments()));

                    WriteProperty(contextObject, evalContext, Name, newMap);
                    result = ReadProperty(contextObject, evalContext, Name);
                }
            }
            else if (typeof(IDictionary) == resultDescriptor)
            {
                if (IsWritableProperty(Name, contextObject, evalContext))
                {
                    var newMap = new Dictionary<object, object>();
                    WriteProperty(contextObject, evalContext, Name, newMap);
                    result = ReadProperty(contextObject, evalContext, Name);
                }
            }
            else if (typeof(IList) == resultDescriptor)
            {
                if (IsWritableProperty(Name, contextObject, evalContext))
                {
                    var newList = new ArrayList();
                    WriteProperty(contextObject, evalContext, Name, newList);
                    result = ReadProperty(contextObject, evalContext, Name);
                }
            }
            else
            {
                // 'simple' object
                try
                {
                    if (IsWritableProperty(Name, contextObject, evalContext))
                    {
                        Type clazz = result.TypeDescriptor;
                        object newObject = ReflectionHelper.GetAccessibleConstructor(clazz).Invoke(Array.Empty<object>());
                        WriteProperty(contextObject, evalContext, Name, newObject);
                        result = ReadProperty(contextObject, evalContext, Name);
                    }
                }
                catch (TargetInvocationException ex)
                {
                    throw new SpelEvaluationException(StartPosition, ex.InnerException, SpelMessage.UnableToDynamicallyCreateObject, result.TypeDescriptor);
                }
                catch (Exception ex)
                {
                    throw new SpelEvaluationException(StartPosition, ex, SpelMessage.UnableToDynamicallyCreateObject, result.TypeDescriptor);
                }
            }
        }

        return result;
    }

    private ITypedValue ReadProperty(ITypedValue contextObject, IEvaluationContext evalContext, string name)
    {
        object targetObject = contextObject.Value;

        if (targetObject == null && IsNullSafe)
        {
            return TypedValue.Null;
        }

        IPropertyAccessor accessorToUse = _cachedReadAccessor;

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

        IList<IPropertyAccessor> accessorsToTry = GetPropertyAccessorsToTry(contextObject.Value, evalContext.PropertyAccessors);

        // Go through the accessors that may be able to resolve it. If they are a cacheable accessor then
        // get the accessor and use it. If they are not cacheable but report they can read the property
        // then ask them to read it
        try
        {
            foreach (IPropertyAccessor acc in accessorsToTry)
            {
                IPropertyAccessor accessor = acc;

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
            throw new SpelEvaluationException(ex, SpelMessage.ExceptionDuringPropertyRead, name, ex.Message);
        }

        if (contextObject.Value == null)
        {
            throw new SpelEvaluationException(SpelMessage.PropertyOrFieldNotReadableOnNull, name);
        }

        throw new SpelEvaluationException(StartPosition, SpelMessage.PropertyOrFieldNotReadable, Name,
            FormatHelper.FormatClassNameForMessage(GetObjectType(contextObject.Value)));
    }

    private void WriteProperty(ITypedValue contextObject, IEvaluationContext evalContext, string name, object newValue)
    {
        if (contextObject.Value == null && IsNullSafe)
        {
            return;
        }

        if (contextObject.Value == null)
        {
            throw new SpelEvaluationException(StartPosition, SpelMessage.PropertyOrFieldNotWritableOnNull, name);
        }

        IPropertyAccessor accessorToUse = _cachedWriteAccessor;

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

        IList<IPropertyAccessor> accessorsToTry = GetPropertyAccessorsToTry(contextObject.Value, evalContext.PropertyAccessors);

        try
        {
            foreach (IPropertyAccessor accessor in accessorsToTry)
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
            throw new SpelEvaluationException(StartPosition, ex, SpelMessage.ExceptionDuringPropertyWrite, name, ex.Message);
        }

        throw new SpelEvaluationException(StartPosition, SpelMessage.PropertyOrFieldNotWritable, name,
            FormatHelper.FormatClassNameForMessage(GetObjectType(contextObject.Value)));
    }

    private IList<IPropertyAccessor> GetPropertyAccessorsToTry(object contextObject, IList<IPropertyAccessor> propertyAccessors)
    {
        Type targetType = contextObject?.GetType();

        var specificAccessors = new List<IPropertyAccessor>();
        var generalAccessors = new List<IPropertyAccessor>();

        foreach (IPropertyAccessor resolver in propertyAccessors)
        {
            IList<Type> targets = resolver.GetSpecificTargetClasses();

            if (targets == null)
            {
                // generic resolver that says it can be used for any type
                generalAccessors.Add(resolver);
            }
            else if (targetType != null)
            {
                foreach (Type clazz in targets)
                {
                    if (clazz == targetType)
                    {
                        specificAccessors.Add(resolver);
                        break;
                    }

                    if (clazz.IsAssignableFrom(targetType))
                    {
                        generalAccessors.Add(resolver);
                    }
                }
            }
        }

        var resolvers = new List<IPropertyAccessor>(specificAccessors);
        generalAccessors.RemoveAll(a => specificAccessors.Contains(a));
        resolvers.AddRange(generalAccessors);
        return resolvers;
    }

    private sealed class AccessorLValue : IValueRef
    {
        private readonly PropertyOrFieldReference _ref;

        private readonly ITypedValue _contextObject;

        private readonly IEvaluationContext _evalContext;

        private readonly bool _autoGrowNullReferences;

        public bool IsWritable => _ref.IsWritableProperty(_ref.Name, _contextObject, _evalContext);

        public AccessorLValue(PropertyOrFieldReference propertyOrFieldReference, ITypedValue activeContextObject, IEvaluationContext evalContext,
            bool autoGrowNullReferences)
        {
            _ref = propertyOrFieldReference;
            _contextObject = activeContextObject;
            _evalContext = evalContext;
            _autoGrowNullReferences = autoGrowNullReferences;
        }

        public ITypedValue GetValue()
        {
            ITypedValue value = _ref.GetValueInternal(_contextObject, _evalContext, _autoGrowNullReferences);
            IPropertyAccessor accessorToUse = _ref._cachedReadAccessor;

            if (accessorToUse is ICompilablePropertyAccessor accessor)
            {
                TypeDescriptor descriptor = _ref.ComputeExitDescriptor(value.Value, accessor.GetPropertyType());
                _ref.SetExitTypeDescriptor(descriptor);
            }

            return value;
        }

        public void SetValue(object newValue)
        {
            _ref.WriteProperty(_contextObject, _evalContext, _ref.Name, newValue);
        }
    }
}
