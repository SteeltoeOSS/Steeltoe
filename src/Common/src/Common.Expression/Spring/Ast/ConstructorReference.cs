// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Common;
using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class ConstructorReference : SpelNode
    {
        private readonly bool _isArrayConstructor = false;

        private readonly SpelNode[] _dimensions;

        // Is this caching safe - passing the expression around will mean this executor is also being passed around
        // The cached executor that may be reused on subsequent evaluations.
        private volatile IConstructorExecutor _cachedExecutor;

        public ConstructorReference(int startPos, int endPos, params SpelNode[] arguments)
            : base(startPos, endPos, arguments)
        {
            _isArrayConstructor = false;
        }

        public ConstructorReference(int startPos, int endPos, SpelNode[] dimensions, params SpelNode[] arguments)
            : base(startPos, endPos, arguments)
        {
            _isArrayConstructor = true;
            _dimensions = dimensions;
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            if (_isArrayConstructor)
            {
                return CreateArray(state);
            }
            else
            {
                return CreateNewInstance(state);
            }
        }

        public override bool IsCompilable()
        {
            if (!(_cachedExecutor is ReflectiveConstructorExecutor) || _exitTypeDescriptor == null)
            {
                return false;
            }

            if (ChildCount > 1)
            {
                for (int c = 1, max = ChildCount; c < max; c++)
                {
                    if (!_children[c].IsCompilable())
                    {
                        return false;
                    }
                }
            }

            var executor = (ReflectiveConstructorExecutor)_cachedExecutor;
            if (executor == null)
            {
                return false;
            }

            var constructor = executor.Constructor;
            return constructor.IsPublic && ReflectionHelper.IsPublic(constructor.DeclaringType);
        }

        public override void GenerateCode(ILGenerator gen, CodeFlow cf)
        {
            var executor = (ReflectiveConstructorExecutor)_cachedExecutor;
            if (executor == null)
            {
                throw new InvalidOperationException("No cached executor");
            }

            var constructor = executor.Constructor;

            // children[0] is the type of the constructor, don't want to include that in argument processing
            var arguments = new SpelNode[_children.Length - 1];
            Array.Copy(_children, 1, arguments, 0, _children.Length - 1);
            GenerateCodeForArguments(gen, cf, constructor, arguments);
            gen.Emit(OpCodes.Newobj, constructor);
            cf.PushDescriptor(_exitTypeDescriptor);
        }

        public override string ToStringAST()
        {
            var sb = new StringBuilder("new ");
            var index = 0;
            sb.Append(GetChild(index++).ToStringAST());
            sb.Append('(');
            for (var i = index; i < ChildCount; i++)
            {
                if (i > index)
                {
                    sb.Append(',');
                }

                sb.Append(GetChild(i).ToStringAST());
            }

            sb.Append(')');
            return sb.ToString();
        }

        private bool HasInitializer => ChildCount > 1;

        private ITypedValue CreateNewInstance(ExpressionState state)
        {
            var arguments = new object[ChildCount - 1];
            var argumentTypes = new List<Type>(ChildCount - 1);
            for (var i = 0; i < arguments.Length; i++)
            {
                var childValue = _children[i + 1].GetValueInternal(state);
                var value = childValue.Value;
                arguments[i] = value;
                var valueType = value?.GetType();
                argumentTypes.Add(valueType);
            }

            var executorToUse = _cachedExecutor;
            if (executorToUse != null)
            {
                try
                {
                    return executorToUse.Execute(state.EvaluationContext, arguments);
                }
                catch (AccessException ex)
                {
                    // Two reasons this can occur:
                    // 1. the method invoked actually threw a real exception
                    // 2. the method invoked was not passed the arguments it expected and has become 'stale'

                    // In the first case we should not retry, in the second case we should see if there is a
                    // better suited method.

                    // To determine which situation it is, the AccessException will contain a cause.
                    // If the cause is an InvocationTargetException, a user exception was thrown inside the constructor.
                    // Otherwise the constructor could not be invoked.
                    if (ex.InnerException is TargetInvocationException)
                    {
                        // User exception was the root cause - exit now
                        var rootCause = ex.InnerException.InnerException;
                        if (rootCause is SystemException)
                        {
                            throw rootCause;
                        }

                        var name = (string)_children[0].GetValueInternal(state).Value;
                        throw new SpelEvaluationException(
                            StartPosition,
                            rootCause,
                            SpelMessage.CONSTRUCTOR_INVOCATION_PROBLEM,
                            name,
                            FormatHelper.FormatMethodForMessage(string.Empty, argumentTypes));
                    }

                    // At this point we know it wasn't a user problem so worth a retry if a better candidate can be found
                    _cachedExecutor = null;
                }
            }

            // Either there was no accessor or it no longer exists
            var typeName = (string)_children[0].GetValueInternal(state).Value;
            if (typeName == null)
            {
                throw new InvalidOperationException("No type name");
            }

            executorToUse = FindExecutorForConstructor(typeName, argumentTypes, state);
            try
            {
                _cachedExecutor = executorToUse;
                if (executorToUse is ReflectiveConstructorExecutor executor)
                {
                    _exitTypeDescriptor = CodeFlow.ToDescriptor(executor.Constructor.DeclaringType);
                }

                return executorToUse.Execute(state.EvaluationContext, arguments);
            }
            catch (AccessException ex)
            {
                throw new SpelEvaluationException(
                    StartPosition,
                    ex,
                    SpelMessage.CONSTRUCTOR_INVOCATION_PROBLEM,
                    typeName,
                    FormatHelper.FormatMethodForMessage(string.Empty, argumentTypes));
            }
        }

        private IConstructorExecutor FindExecutorForConstructor(string typeName, List<Type> argumentTypes, ExpressionState state)
        {
            var evalContext = state.EvaluationContext;
            var ctorResolvers = evalContext.ConstructorResolvers;
            foreach (var ctorResolver in ctorResolvers)
            {
                try
                {
                    var ce = ctorResolver.Resolve(state.EvaluationContext, typeName, argumentTypes);
                    if (ce != null)
                    {
                        return ce;
                    }
                }
                catch (AccessException ex)
                {
                    throw new SpelEvaluationException(
                        StartPosition,
                        ex,
                        SpelMessage.CONSTRUCTOR_INVOCATION_PROBLEM,
                        typeName,
                        FormatHelper.FormatMethodForMessage(string.Empty, argumentTypes));
                }
            }

            throw new SpelEvaluationException(
                StartPosition,
                SpelMessage.CONSTRUCTOR_NOT_FOUND,
                typeName,
                FormatHelper.FormatMethodForMessage(string.Empty, argumentTypes));
        }

        private TypedValue CreateArray(ExpressionState state)
        {
            // First child gives us the array type which will either be a primitive or reference type
            var intendedArrayType = GetChild(0).GetValue(state);
            if (!(intendedArrayType is string))
            {
                throw new SpelEvaluationException(
                    GetChild(0).StartPosition,
                    SpelMessage.TYPE_NAME_EXPECTED_FOR_ARRAY_CONSTRUCTION,
                    FormatHelper.FormatClassNameForMessage(intendedArrayType?.GetType()));
            }

            var type = (string)intendedArrayType;
            Type componentType;
            var arrayTypeCode = SpelTypeCode.ForName(type);
            if (arrayTypeCode == SpelTypeCode.OBJECT)
            {
                componentType = state.FindType(type);
            }
            else
            {
                componentType = arrayTypeCode.Type;
            }

            object newArray;
            if (!HasInitializer)
            {
                // Confirm all dimensions were specified (for example [3][][5] is missing the 2nd dimension)
                if (_dimensions != null)
                {
                    foreach (var dimension in _dimensions)
                    {
                        if (dimension == null)
                        {
                            throw new SpelEvaluationException(StartPosition, SpelMessage.MISSING_ARRAY_DIMENSION);
                        }
                    }
                }
                else
                {
                    throw new SpelEvaluationException(StartPosition, SpelMessage.MISSING_ARRAY_DIMENSION);
                }

                var typeConverter = state.EvaluationContext.TypeConverter;

                // Shortcut for 1 dimensional
                if (_dimensions.Length == 1)
                {
                    var o = _dimensions[0].GetTypedValue(state);
                    var arraySize = ExpressionUtils.ToInt(typeConverter, o);
                    newArray = Array.CreateInstance(componentType, arraySize);
                }
                else
                {
                    // Multi-dimensional - hold onto your hat!
                    var dims = new int[_dimensions.Length];
                    for (var d = 0; d < _dimensions.Length; d++)
                    {
                        var o = _dimensions[d].GetTypedValue(state);
                        dims[d] = ExpressionUtils.ToInt(typeConverter, o);
                    }

                    newArray = Array.CreateInstance(componentType, dims);
                }
            }
            else
            {
                // There is an initializer
                if (_dimensions == null || _dimensions.Length > 1)
                {
                    // There is an initializer but this is a multi-dimensional array (e.g. new int[][]{{1,2},{3,4}}) - this
                    // is not currently supported
                    throw new SpelEvaluationException(StartPosition, SpelMessage.MULTIDIM_ARRAY_INITIALIZER_NOT_SUPPORTED);
                }

                var typeConverter = state.EvaluationContext.TypeConverter;
                var initializer = (InlineList)GetChild(1);

                // If a dimension was specified, check it matches the initializer length
                if (_dimensions[0] != null)
                {
                    var dValue = _dimensions[0].GetTypedValue(state);
                    var i = ExpressionUtils.ToInt(typeConverter, dValue);
                    if (i != initializer.ChildCount)
                    {
                        throw new SpelEvaluationException(StartPosition, SpelMessage.INITIALIZER_LENGTH_INCORRECT);
                    }
                }

                // Build the array and populate it
                var arraySize = initializer.ChildCount;
                newArray = Array.CreateInstance(componentType, arraySize);
                if (arrayTypeCode == SpelTypeCode.OBJECT)
                {
                    PopulateReferenceTypeArray(state, newArray, typeConverter, initializer, componentType);
                }
                else if (arrayTypeCode == SpelTypeCode.BOOLEAN)
                {
                    PopulateBooleanArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.BYTE)
                {
                    PopulateByteArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.SBYTE)
                {
                    PopulateSByteArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.CHAR)
                {
                    PopulateCharArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.DOUBLE)
                {
                    PopulateDoubleArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.FLOAT)
                {
                    PopulateFloatArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.INT)
                {
                    PopulateIntArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.UINT)
                {
                    PopulateUIntArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.LONG)
                {
                    PopulateLongArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.ULONG)
                {
                    PopulateULongArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.SHORT)
                {
                    PopulateShortArray(state, newArray, typeConverter, initializer);
                }
                else if (arrayTypeCode == SpelTypeCode.USHORT)
                {
                    PopulateUShortArray(state, newArray, typeConverter, initializer);
                }
                else
                {
                    throw new InvalidOperationException(arrayTypeCode.Name);
                }
            }

            return new TypedValue(newArray);
        }

        private void PopulateReferenceTypeArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer, Type componentType)
        {
            var newObjectArray = (object[])newArray;
            for (var i = 0; i < newObjectArray.Length; i++)
            {
                var elementNode = initializer.GetChild(i);
                var arrayEntry = elementNode.GetValue(state);
                newObjectArray[i] = typeConverter.ConvertValue(arrayEntry, arrayEntry?.GetType(), componentType);
            }
        }

        private void PopulateByteArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newByteArray = (byte[])newArray;
            for (var i = 0; i < newByteArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newByteArray[i] = ExpressionUtils.ToByte(typeConverter, typedValue);
            }
        }

        private void PopulateSByteArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newByteArray = (sbyte[])newArray;
            for (var i = 0; i < newByteArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newByteArray[i] = ExpressionUtils.ToSByte(typeConverter, typedValue);
            }
        }

        private void PopulateFloatArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newFloatArray = (float[])newArray;
            for (var i = 0; i < newFloatArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newFloatArray[i] = ExpressionUtils.ToFloat(typeConverter, typedValue);
            }
        }

        private void PopulateDoubleArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newDoubleArray = (double[])newArray;
            for (var i = 0; i < newDoubleArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newDoubleArray[i] = ExpressionUtils.ToDouble(typeConverter, typedValue);
            }
        }

        private void PopulateShortArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newShortArray = (short[])newArray;
            for (var i = 0; i < newShortArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newShortArray[i] = ExpressionUtils.ToShort(typeConverter, typedValue);
            }
        }

        private void PopulateUShortArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newShortArray = (ushort[])newArray;
            for (var i = 0; i < newShortArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newShortArray[i] = ExpressionUtils.ToUShort(typeConverter, typedValue);
            }
        }

        private void PopulateLongArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newLongArray = (long[])newArray;
            for (var i = 0; i < newLongArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newLongArray[i] = ExpressionUtils.ToLong(typeConverter, typedValue);
            }
        }

        private void PopulateULongArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newLongArray = (ulong[])newArray;
            for (var i = 0; i < newLongArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newLongArray[i] = ExpressionUtils.ToULong(typeConverter, typedValue);
            }
        }

        private void PopulateCharArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newCharArray = (char[])newArray;
            for (var i = 0; i < newCharArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newCharArray[i] = ExpressionUtils.ToChar(typeConverter, typedValue);
            }
        }

        private void PopulateBooleanArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newBooleanArray = (bool[])newArray;
            for (var i = 0; i < newBooleanArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newBooleanArray[i] = ExpressionUtils.ToBoolean(typeConverter, typedValue);
            }
        }

        private void PopulateIntArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newIntArray = (int[])newArray;
            for (var i = 0; i < newIntArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newIntArray[i] = ExpressionUtils.ToInt(typeConverter, typedValue);
            }
        }

        private void PopulateUIntArray(ExpressionState state, object newArray, ITypeConverter typeConverter, InlineList initializer)
        {
            var newIntArray = (uint[])newArray;
            for (var i = 0; i < newIntArray.Length; i++)
            {
                var typedValue = initializer.GetChild(i).GetTypedValue(state);
                newIntArray[i] = ExpressionUtils.ToUInt(typeConverter, typedValue);
            }
        }
    }
}
