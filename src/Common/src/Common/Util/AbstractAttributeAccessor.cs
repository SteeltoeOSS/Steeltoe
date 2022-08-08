// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public abstract class AbstractAttributeAccessor : IAttributeAccessor
{
    private readonly Dictionary<string, object> _attributes = new();

    public virtual string[] AttributeNames
    {
#pragma warning disable S2365 // Properties should not make collection or array copies
        get
        {
            return _attributes.Keys.ToArray();
        }
#pragma warning restore S2365 // Properties should not make collection or array copies
    }

    public virtual void SetAttribute(string name, object value)
    {
        ArgumentGuard.NotNull(name);

        if (value != null)
        {
            _attributes[name] = value;
        }
        else
        {
            RemoveAttribute(name);
        }
    }

    public virtual object GetAttribute(string name)
    {
        ArgumentGuard.NotNull(name);

        _attributes.TryGetValue(name, out object result);
        return result;
    }

    public virtual object RemoveAttribute(string name)
    {
        ArgumentGuard.NotNull(name);

        _attributes.TryGetValue(name, out object original);
        _attributes.Remove(name);
        return original;
    }

    public virtual bool HasAttribute(string name)
    {
        ArgumentGuard.NotNull(name);

        return _attributes.ContainsKey(name);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not AbstractAttributeAccessor accessor)
        {
            return false;
        }

        if (accessor._attributes.Count != _attributes.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, object> kvp in _attributes)
        {
            if (!accessor._attributes.TryGetValue(kvp.Key, out object value2))
            {
                return false;
            }

            if (!kvp.Value.Equals(value2))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        return _attributes.GetHashCode();
    }

    protected virtual void CopyAttributesFrom(IAttributeAccessor source)
    {
        ArgumentGuard.NotNull(source);

        string[] attributeNames = source.AttributeNames;

        foreach (string attributeName in attributeNames)
        {
            SetAttribute(attributeName, source.GetAttribute(attributeName));
        }
    }
}
