// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Connector.Services;

public class Tags
{
    public IEnumerable<string> Values { get; protected internal set; }

    public Tags(string tag)
        : this(new[]
        {
            tag
        })
    {
    }

    public Tags(string[] tags)
    {
        Values = tags ?? Array.Empty<string>();
    }

    internal Tags()
    {
    }

    public bool ContainsOne(IEnumerable<string> tags)
    {
        return tags != null && Values != null && tags.Intersect(Values).Any();
    }

    public bool Contains(string tag)
    {
        if (Values == null)
        {
            return false;
        }

        return Values.Contains(tag);
    }

    public bool StartsWith(string tag)
    {
        if (tag != null && Values != null)
        {
            foreach (string t in Values)
            {
                if (tag.StartsWith(t))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
