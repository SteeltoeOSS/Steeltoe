// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal.Spring.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast
{
    public class Selection : SpelNode
    {
        public const int ALL = 0;
        public const int FIRST = 1;
        public const int LAST = 2;

        private readonly int _variant;

        private readonly bool _nullSafe;

        public Selection(bool nullSafe, int variant, int startPos, int endPos, SpelNode expression)
            : base(startPos, endPos, expression)
        {
            _nullSafe = nullSafe;
            _variant = variant;
        }

        public override ITypedValue GetValueInternal(ExpressionState state)
        {
            return GetValueRef(state).GetValue();
        }

        public override string ToStringAST()
        {
            return GetPrefix() + GetChild(0).ToStringAST() + "]";
        }

        protected internal override IValueRef GetValueRef(ExpressionState state)
        {
            var op = state.GetActiveContextObject();
            var operand = op.Value;
            var selectionCriteria = _children[0];

            if (operand is System.Collections.IDictionary)
            {
                var mapdata = (System.Collections.IDictionary)operand;

                // TODO don't lose generic info for the new map
                var result = new Dictionary<object, object>();
                object lastKey = null;

                foreach (DictionaryEntry entry in mapdata)
                {
                    try
                    {
                        var kvPair = new TypedValue(entry);
                        state.PushActiveContextObject(kvPair);
                        state.EnterScope();
                        var val = selectionCriteria.GetValueInternal(state).Value;
                        if (val is bool)
                        {
                            if ((bool)val)
                            {
                                if (_variant == FIRST)
                                {
                                    result[entry.Key] = entry.Value;
                                    return new TypedValueHolderValueRef(new TypedValue(result), this);
                                }

                                result[entry.Key] = entry.Value;
                                lastKey = entry.Key;
                            }
                        }
                        else
                        {
                            throw new SpelEvaluationException(selectionCriteria.StartPosition, SpelMessage.RESULT_OF_SELECTION_CRITERIA_IS_NOT_BOOLEAN);
                        }
                    }
                    finally
                    {
                        state.PopActiveContextObject();
                        state.ExitScope();
                    }
                }

                if ((_variant == FIRST || _variant == LAST) && result.Count == 0)
                {
                    return new TypedValueHolderValueRef(new TypedValue(null), this);
                }

                if (_variant == LAST)
                {
                    var resultMap = new Dictionary<object, object>();
                    result.TryGetValue(lastKey, out var lastValue);
                    resultMap[lastKey] = lastValue;
                    return new TypedValueHolderValueRef(new TypedValue(resultMap), this);
                }

                return new TypedValueHolderValueRef(new TypedValue(result), this);
            }

            if (operand is IEnumerable)
            {
                var operandAsArray = operand as Array;
                var data = operand as IEnumerable;

                var result = new List<object>();
                var index = 0;
                foreach (var element in data)
                {
                    try
                    {
                        state.PushActiveContextObject(new TypedValue(element));
                        state.EnterScope("index", index);
                        var val = selectionCriteria.GetValueInternal(state).Value;
                        if (val is bool)
                        {
                            if ((bool)val)
                            {
                                if (_variant == FIRST)
                                {
                                    return new TypedValueHolderValueRef(new TypedValue(element), this);
                                }

                                result.Add(element);
                            }
                        }
                        else
                        {
                            throw new SpelEvaluationException(selectionCriteria.StartPosition, SpelMessage.RESULT_OF_SELECTION_CRITERIA_IS_NOT_BOOLEAN);
                        }

                        index++;
                    }
                    finally
                    {
                        state.ExitScope();
                        state.PopActiveContextObject();
                    }
                }

                if ((_variant == FIRST || _variant == LAST) && result.Count == 0)
                {
                    return NullValueRef.INSTANCE;
                }

                if (_variant == LAST)
                {
                    var lastElem = result == null || result.Count == 0 ? null : result[result.Count - 1];
                    return new TypedValueHolderValueRef(new TypedValue(lastElem), this);
                }

                if (operand is IEnumerable && operandAsArray == null)
                {
                    return new TypedValueHolderValueRef(new TypedValue(result), this);
                }

                // Array
                if (operandAsArray != null)
                {
                    Type elementType = null;
                    var typeDesc = op.TypeDescriptor;
                    if (typeDesc != null)
                    {
                        elementType = ReflectionHelper.GetElementTypeDescriptor(typeDesc);

                        // if (elementTypeDesc != null)
                        // {
                        //    elementType = ClassUtils.resolvePrimitiveIfNecessary(elementTypeDesc);
                        // }
                    }

                    if (elementType == null)
                    {
                        throw new InvalidOperationException("Unresolvable element type");
                    }

                    var resultArray = Array.CreateInstance(elementType, result.Count);
                    Array.Copy(result.ToArray(), 0, resultArray, 0, result.Count);
                    return new TypedValueHolderValueRef(new TypedValue(resultArray), this);
                }
            }

            if (operand == null)
            {
                if (_nullSafe)
                {
                    return NullValueRef.INSTANCE;
                }

                throw new SpelEvaluationException(StartPosition, SpelMessage.INVALID_TYPE_FOR_SELECTION, "null");
            }

            throw new SpelEvaluationException(StartPosition, SpelMessage.INVALID_TYPE_FOR_SELECTION, operand.GetType().FullName);
        }

        private string GetPrefix()
        {
            switch (_variant)
            {
                case ALL: return "?[";
                case FIRST: return "^[";
                case LAST: return "$[";
            }

            return string.Empty;
        }
    }
}
