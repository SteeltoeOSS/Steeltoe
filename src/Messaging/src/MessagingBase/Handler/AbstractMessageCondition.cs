// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Text;

namespace Steeltoe.Messaging.Handler;

public abstract class AbstractMessageCondition<T> : IMessageCondition<T>
{
    public abstract T Combine(T other);

    public abstract int CompareTo(T other, IMessage message);

    public abstract T GetMatchingCondition(IMessage message);

    public override bool Equals(object other)
    {
        if (this == other)
        {
            return true;
        }

        if (other == null || GetType() != other.GetType())
        {
            return false;
        }

        var content1 = GetContent();
        var content2 = GetContent();

        if (content1.Count != content2.Count)
        {
            return false;
        }

        for (var i = 0; i < content1.Count; i++)
        {
            if (!content1[i].Equals(content2[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        return GetContent().GetHashCode();
    }

    public override string ToString()
    {
        var infix = GetToStringInfix();

        var joiner = new StringBuilder("[");
        foreach (var expression in GetContent())
        {
            joiner.Append(expression.ToString() + infix);
        }

        return joiner.ToString(0, joiner.Length - 1) + "]";
    }

    protected abstract IList GetContent();

    protected abstract string GetToStringInfix();
}