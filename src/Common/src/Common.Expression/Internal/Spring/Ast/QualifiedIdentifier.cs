// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class QualifiedIdentifier : SpelNode
{
    private ITypedValue _value;

    public QualifiedIdentifier(int startPos, int endPos, params SpelNode[] operands)
        : base(startPos, endPos, operands)
    {
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        // Cache the concatenation of child identifiers
        if (_value == null)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < ChildCount; i++)
            {
                object value = children[i].GetValueInternal(state).Value;

                if (i > 0 && (value == null || !value.ToString().StartsWith("$")))
                {
                    sb.Append('.');
                }

                sb.Append(value);
            }

            _value = new TypedValue(sb.ToString());
        }

        return _value;
    }

    public override string ToStringAst()
    {
        var sb = new StringBuilder();

        if (_value != null)
        {
            sb.Append(_value.Value);
        }
        else
        {
            for (int i = 0; i < ChildCount; i++)
            {
                if (i > 0)
                {
                    sb.Append('.');
                }

                sb.Append(GetChild(i).ToStringAst());
            }
        }

        return sb.ToString();
    }
}
