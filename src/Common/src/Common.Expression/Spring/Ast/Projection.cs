﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class Projection : SpelNode
    {
        private readonly bool _nullSafe;

        public Projection(bool nullSafe, int startPos, int endPos, SpelNode expression)
            : base(startPos, endPos, expression)
        {
            _nullSafe = nullSafe;
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            return GetValueRef(state).GetValue();
        }

        public override string ToStringAST()
        {
            return "![" + GetChild(0).ToStringAST() + "]";
        }

        protected internal override IValueRef GetValueRef(ExpressionState state)
        {
            var op = state.GetActiveContextObject();

            var operand = op.Value;
            var operandAsArray = operand as Array;

            // TypeDescriptor operandTypeDescriptor = op.getTypeDescriptor();

            // When the input is a map, we push a special context object on the stack
            // before calling the specified operation. This special context object
            // has two fields 'key' and 'value' that refer to the map entries key
            // and value, and they can be referenced in the operation
            // eg. {'a':'y','b':'n'}.![value=='y'?key:null]" == ['a', null]
            if (operand is IDictionary)
            {
                var mapData = (IDictionary)operand;
                var result = new List<object>();
                foreach (var entry in mapData)
                {
                    try
                    {
                        state.PushActiveContextObject(new TypedValue(entry));
                        state.EnterScope();
                        result.Add(_children[0].GetValueInternal(state).Value);
                    }
                    finally
                    {
                        state.PopActiveContextObject();
                        state.ExitScope();
                    }
                }

                return new TypedValueHolderValueRef(new TypedValue(result), this);  // TODO unable to build correct type descriptor
            }

            if (operand is IEnumerable)
            {
                var data = operand as IEnumerable;

                var result = new List<object>();
                Type arrayElementType = null;
                foreach (var element in data)
                {
                    try
                    {
                        state.PushActiveContextObject(new TypedValue(element));
                        state.EnterScope("index", result.Count);
                        var value = _children[0].GetValueInternal(state).Value;
                        if (value != null && operandAsArray != null)
                        {
                            arrayElementType = DetermineCommonType(arrayElementType, value.GetType());
                        }

                        result.Add(value);
                    }
                    finally
                    {
                        state.ExitScope();
                        state.PopActiveContextObject();
                    }
                }

                if (operandAsArray != null)
                {
                    if (arrayElementType == null)
                    {
                        arrayElementType = typeof(object);
                    }

                    var resultArray = Array.CreateInstance(arrayElementType, result.Count);
                    Array.Copy(result.ToArray(), 0, resultArray, 0, result.Count);
                    return new TypedValueHolderValueRef(new TypedValue(resultArray), this);
                }

                return new TypedValueHolderValueRef(new TypedValue(result), this);
            }

            if (operand == null)
            {
                if (_nullSafe)
                {
                    return NullValueRef.INSTANCE;
                }

                throw new SpelEvaluationException(StartPosition, SpelMessage.PROJECTION_NOT_SUPPORTED_ON_TYPE, "null");
            }

            throw new SpelEvaluationException(StartPosition, SpelMessage.PROJECTION_NOT_SUPPORTED_ON_TYPE, operand.GetType().FullName);
        }

        private Type DetermineCommonType(Type oldType, Type newType)
        {
            if (oldType == null)
            {
                return newType;
            }

            if (oldType.IsAssignableFrom(newType))
            {
                return oldType;
            }

            var nextType = newType;
            while (nextType != typeof(object))
            {
                if (nextType.IsAssignableFrom(oldType))
                {
                    return nextType;
                }

                nextType = nextType.BaseType;
            }

            var ifaces = newType.FindInterfaces((m, c) => true, null);
            foreach (var nextInterface in ifaces)
            {
                if (nextInterface.IsAssignableFrom(oldType))
                {
                    return nextInterface;
                }
            }

            return typeof(object);
        }
    }
}
