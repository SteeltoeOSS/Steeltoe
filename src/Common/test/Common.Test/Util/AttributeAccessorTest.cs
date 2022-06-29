// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Common.Util.Test;

public class AttributeAccessorTest
{
    private const string NAME = "foo";

    private const string VALUE = "bar";

    private readonly SimpleAttributeAccessor _attributeAccessor = new ();

    [Fact]
    public void SetAndGet()
    {
        _attributeAccessor.SetAttribute(NAME, VALUE);
        Assert.Equal(VALUE, _attributeAccessor.GetAttribute(NAME));
    }

    [Fact]
    public void SetAndHas()
    {
        Assert.False(_attributeAccessor.HasAttribute(NAME));
        _attributeAccessor.SetAttribute(NAME, VALUE);
        Assert.True(_attributeAccessor.HasAttribute(NAME));
    }

    [Fact]
    public void Remove()
    {
        Assert.False(_attributeAccessor.HasAttribute(NAME));
        _attributeAccessor.SetAttribute(NAME, VALUE);
        Assert.Equal(VALUE, _attributeAccessor.RemoveAttribute(NAME));
        Assert.False(_attributeAccessor.HasAttribute(NAME));
    }

    [Fact]
    public void AttributeNames()
    {
        _attributeAccessor.SetAttribute(NAME, VALUE);
        _attributeAccessor.SetAttribute("abc", "123");
        var attributeNames = _attributeAccessor.AttributeNames;
        Assert.Contains(NAME, attributeNames);
        Assert.Contains("abc", attributeNames);
    }

    private sealed class SimpleAttributeAccessor : AbstractAttributeAccessor
    {
    }
}
