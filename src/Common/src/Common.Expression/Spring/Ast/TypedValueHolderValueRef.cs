// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class TypedValueHolderValueRef : IValueRef
{
    private readonly ITypedValue _typedValue;
    private readonly SpelNode _node;  // used only for error reporting

    public TypedValueHolderValueRef(ITypedValue typedValue, SpelNode node)
    {
        _typedValue = typedValue;
        _node = node;
    }

    public ITypedValue GetValue() => _typedValue;

    public void SetValue(object newValue)
    {
        throw new SpelEvaluationException(_node.StartPosition, SpelMessage.NOT_ASSIGNABLE, _node.ToStringAST());
    }

    public bool IsWritable => false;
}