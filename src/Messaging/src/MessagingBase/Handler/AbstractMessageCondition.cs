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

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not AbstractMessageCondition<T> other || GetType() != obj.GetType())
        {
            return false;
        }

        var thisContent = GetContent();
        var otherContent = other.GetContent();

        if (thisContent.Count != otherContent.Count)
        {
            return false;
        }

        for (var i = 0; i < thisContent.Count; i++)
        {
            if (!Equals(thisContent[i], otherContent[i]))
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
            joiner.Append(expression + infix);
        }

        return $"{joiner.ToString(0, joiner.Length - 1)}]";
    }

    protected abstract IList GetContent();

    protected abstract string GetToStringInfix();
}
