// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Common.Util.Test;

public class AttributeAccessorTest
{
    private const string Name = "foo";

    private const string Value = "bar";

    private readonly SimpleAttributeAccessor _attributeAccessor = new();

    [Fact]
    public void SetAndGet()
    {
        _attributeAccessor.SetAttribute(Name, Value);
        Assert.Equal(Value, _attributeAccessor.GetAttribute(Name));
    }

    [Fact]
    public void SetAndHas()
    {
        Assert.False(_attributeAccessor.HasAttribute(Name));
        _attributeAccessor.SetAttribute(Name, Value);
        Assert.True(_attributeAccessor.HasAttribute(Name));
    }

    [Fact]
    public void Remove()
    {
        Assert.False(_attributeAccessor.HasAttribute(Name));
        _attributeAccessor.SetAttribute(Name, Value);
        Assert.Equal(Value, _attributeAccessor.RemoveAttribute(Name));
        Assert.False(_attributeAccessor.HasAttribute(Name));
    }

    [Fact]
    public void AttributeNames()
    {
        _attributeAccessor.SetAttribute(Name, Value);
        _attributeAccessor.SetAttribute("abc", "123");
        string[] attributeNames = _attributeAccessor.AttributeNames;
        Assert.Contains(Name, attributeNames);
        Assert.Contains("abc", attributeNames);
    }

    private sealed class SimpleAttributeAccessor : AbstractAttributeAccessor
    {
    }
}
