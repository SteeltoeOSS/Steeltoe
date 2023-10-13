// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        IValueRef valueRef = GetValueRef(state);
        ITypedValue result = valueRef.GetValue();
        exitTypeDescriptor = children[^1].ExitDescriptor;
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

    public override string ToStringAst()
    {
        var strings = new List<string>();

        for (int i = 0; i < ChildCount; i++)
        {
            strings.Add(GetChild(i).ToStringAst());
        }

        return string.Join(".", strings);
    }

    public override bool IsCompilable()
    {
        foreach (SpelNode child in children)
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
        foreach (SpelNode child in children)
        {
            child.GenerateCode(gen, cf);
        }

        cf.PushDescriptor(exitTypeDescriptor);
    }

    protected internal override IValueRef GetValueRef(ExpressionState state)
    {
        if (ChildCount == 1)
        {
            return children[0].GetValueRef(state);
        }

        SpelNode nextNode = children[0];

        try
        {
            ITypedValue result = nextNode.GetValueInternal(state);
            int cc = ChildCount;

            for (int i = 1; i < cc - 1; i++)
            {
                try
                {
                    state.PushActiveContextObject(result);
                    nextNode = children[i];

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
                nextNode = children[cc - 1];
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
