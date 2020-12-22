// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Steeltoe.Common.Expression.Spring.Ast
{
    public class InlineMap : SpelNode
    {
        // If the map is purely literals, it is a constant value and can be computed and cached
        private ITypedValue _constant;

        public InlineMap(int startPos, int endPos, params SpelNode[] args)
            : base(startPos, endPos, args)
        {
            CheckIfConstant();
        }

        public override ITypedValue GetValueInternal(ExpressionState expressionState)
        {
            if (_constant != null)
            {
                return _constant;
            }
            else
            {
                var returnValue = new Dictionary<object, object>();
                var childcount = ChildCount;
                for (var c = 0; c < childcount; c++)
                {
                    // TODO allow for key being PropertyOrFieldReference like Indexer on maps
                    var keyChild = GetChild(c++);
                    object key = null;
                    if (keyChild is PropertyOrFieldReference)
                    {
                        var reference = (PropertyOrFieldReference)keyChild;
                        key = reference.Name;
                    }
                    else
                    {
                        key = keyChild.GetValue(expressionState);
                    }

                    var value = GetChild(c).GetValue(expressionState);
                    returnValue[key] = value;
                }

                return new TypedValue(returnValue);
            }
        }

        public override string ToStringAST()
        {
            var sb = new StringBuilder("{");
            var count = ChildCount;
            for (var c = 0; c < count; c++)
            {
                if (c > 0)
                {
                    sb.Append(",");
                }

                sb.Append(GetChild(c++).ToStringAST());
                sb.Append(":");
                sb.Append(GetChild(c).ToStringAST());
            }

            sb.Append("}");
            return sb.ToString();
        }

        public bool IsConstant => _constant != null;

        public IDictionary<object, object> GetConstantValue()
        {
            if (_constant == null)
            {
                throw new InvalidOperationException("No constant");
            }

            return (IDictionary<object, object>)_constant.Value;
        }

        private void CheckIfConstant()
        {
            var isConstant = true;
            for (int c = 0, max = ChildCount; c < max; c++)
            {
                var child = GetChild(c);
                if (!(child is Literal))
                {
                    if (child is InlineList)
                    {
                        var inlineList = (InlineList)child;
                        if (!inlineList.IsConstant)
                        {
                            isConstant = false;
                            break;
                        }
                    }
                    else if (child is InlineMap)
                    {
                        var inlineMap = (InlineMap)child;
                        if (!inlineMap.IsConstant)
                        {
                            isConstant = false;
                            break;
                        }
                    }
                    else if (!(c % 2 == 0 && child is PropertyOrFieldReference))
                    {
                        isConstant = false;
                        break;
                    }
                }
            }

            if (isConstant)
            {
                var constantMap = new Dictionary<object, object>();
                var childCount = ChildCount;
                for (var c = 0; c < childCount; c++)
                {
                    var keyChild = GetChild(c++);
                    var valueChild = GetChild(c);
                    object key = null;
                    object value = null;
                    if (keyChild is Literal)
                    {
                        key = ((Literal)keyChild).GetLiteralValue().Value;
                    }
                    else if (keyChild is PropertyOrFieldReference)
                    {
                        key = ((PropertyOrFieldReference)keyChild).Name;
                    }
                    else
                    {
                        return;
                    }

                    if (valueChild is Literal)
                    {
                        value = ((Literal)valueChild).GetLiteralValue().Value;
                    }
                    else if (valueChild is InlineList)
                    {
                        value = ((InlineList)valueChild).GetConstantValue();
                    }
                    else if (valueChild is InlineMap)
                    {
                        value = ((InlineMap)valueChild).GetConstantValue();
                    }

                    constantMap[key] = value;
                }

                _constant = new TypedValue(new ReadOnlyDictionary<object, object>(constantMap));
            }
        }
    }
}
