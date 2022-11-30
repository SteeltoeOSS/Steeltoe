// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class InlineMap : SpelNode
{
    // If the map is purely literals, it is a constant value and can be computed and cached
    private ITypedValue _constant;

    public bool IsConstant => _constant != null;

    public InlineMap(int startPos, int endPos, params SpelNode[] args)
        : base(startPos, endPos, args)
    {
        CheckIfConstant();
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        if (_constant != null)
        {
            return _constant;
        }

        var returnValue = new Dictionary<object, object>();
        int childCount = ChildCount;

        for (int c = 0; c < childCount; c++)
        {
            // Allow for key being PropertyOrFieldReference like Indexer on maps
            ISpelNode keyChild = GetChild(c++);
            object key = null;

            if (keyChild is PropertyOrFieldReference reference)
            {
                key = reference.Name;
            }
            else
            {
                key = keyChild.GetValue(state);
            }

            object value = GetChild(c).GetValue(state);
            returnValue[key] = value;
        }

        return new TypedValue(returnValue);
    }

    public override string ToStringAst()
    {
        var sb = new StringBuilder("{");
        int count = ChildCount;

        for (int c = 0; c < count; c++)
        {
            if (c > 0)
            {
                sb.Append(',');
            }

            sb.Append(GetChild(c++).ToStringAst());
            sb.Append(':');
            sb.Append(GetChild(c).ToStringAst());
        }

        sb.Append('}');
        return sb.ToString();
    }

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
        bool isConstant = true;

        for (int c = 0, max = ChildCount; c < max; c++)
        {
            ISpelNode child = GetChild(c);

            if (child is not Literal)
            {
                if (child is InlineList inlineList)
                {
                    if (!inlineList.IsConstant)
                    {
                        isConstant = false;
                        break;
                    }
                }
                else if (child is InlineMap inlineMap)
                {
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
            int childCount = ChildCount;

            for (int c = 0; c < childCount; c++)
            {
                ISpelNode keyChild = GetChild(c++);
                ISpelNode valueChild = GetChild(c);
                object key = null;
                object value = null;

                if (keyChild is Literal literal)
                {
                    key = literal.GetLiteralValue().Value;
                }
                else if (keyChild is PropertyOrFieldReference reference)
                {
                    key = reference.Name;
                }
                else
                {
                    return;
                }

                if (valueChild is Literal literal1)
                {
                    value = literal1.GetLiteralValue().Value;
                }
                else if (valueChild is InlineList list)
                {
                    value = list.GetConstantValue();
                }
                else if (valueChild is InlineMap map)
                {
                    value = map.GetConstantValue();
                }

                constantMap[key] = value;
            }

            _constant = new TypedValue(new ReadOnlyDictionary<object, object>(constantMap));
        }
    }
}