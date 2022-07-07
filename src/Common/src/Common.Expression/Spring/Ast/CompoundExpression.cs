// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Steeltoe.Common.Expression.Internal.Spring.Ast;

public class CompoundExpression : SpelNode
{
    public CompoundExpression(int startPos, int endPos, params SpelNode[] expressionComponents)
        : base(startPos, endPos, expressionComponents)
    {
        if (expressionComponents.Length < 2)
        {
            throw new InvalidOperationException($"Do not build compound expressions with less than two entries: {expressionComponents.Length}");
        }
    }

    public override ITypedValue GetValueInternal(ExpressionState state)
    {
        var valueRef = GetValueRef(state);
        var result = valueRef.GetValue();
        _exitTypeDescriptor = _children[_children.Length - 1].ExitDescriptor;
        return result;
    }

    public override void SetValue(ExpressionState state, object newValue)
    {
        GetValueRef(state).SetValue(newValue);
    }

    public override bool IsWritable(ExpressionState state)
    {
        return GetValueRef(state).IsWritable;
    }

    public override string ToStringAST()
    {
        var strings = new List<string>();
        for (var i = 0; i < ChildCount; i++)
        {
            strings.Add(GetChild(i).ToStringAST());
        }

        return string.Join(".", strings);
    }

    public override bool IsCompilable()
    {
        foreach (var child in _children)
        {
            if (!child.IsCompilable())
            {
                return false;
            }
        }

        return true;
    }

    public override void GenerateCode(ILGenerator gen, CodeFlow cf)
    {
        foreach (var child in _children)
        {
            child.GenerateCode(gen, cf);
        }

        cf.PushDescriptor(_exitTypeDescriptor);
    }

    protected internal override IValueRef GetValueRef(ExpressionState state)
    {
        if (ChildCount == 1)
        {
            return _children[0].GetValueRef(state);
        }

        var nextNode = _children[0];
        try
        {
            var result = nextNode.GetValueInternal(state);
            var cc = ChildCount;
            for (var i = 1; i < cc - 1; i++)
            {
                try
                {
                    state.PushActiveContextObject(result);
                    nextNode = _children[i];

                    result = nextNode.GetValueInternal(state);
                }
                finally
                {
                    state.PopActiveContextObject();
                }
            }

            try
            {
                state.PushActiveContextObject(result);
                nextNode = _children[cc - 1];
                return nextNode.GetValueRef(state);
            }
            finally
            {
                state.PopActiveContextObject();
            }
        }
        catch (SpelEvaluationException ex)
        {
            // Correct the position for the error before re-throwing
            ex.Position = nextNode.StartPosition;
            throw;
        }
    }
}
