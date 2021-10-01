// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Common.Util.Test
{
    public class AttributeAccessorTest
    {
        private const string NAME = "foo";

        private const string VALUE = "bar";

        private SimpleAttributeAccessor attributeAccessor = new SimpleAttributeAccessor();

        [Fact]
        public void SetAndGet()
        {
            attributeAccessor.SetAttribute(NAME, VALUE);
            Assert.Equal(VALUE, attributeAccessor.GetAttribute(NAME));
        }

        [Fact]
        public void SetAndHas()
        {
            Assert.False(attributeAccessor.HasAttribute(NAME));
            attributeAccessor.SetAttribute(NAME, VALUE);
            Assert.True(attributeAccessor.HasAttribute(NAME));
        }

        [Fact]
        public void Remove()
        {
            Assert.False(attributeAccessor.HasAttribute(NAME));
            attributeAccessor.SetAttribute(NAME, VALUE);
            Assert.Equal(VALUE, attributeAccessor.RemoveAttribute(NAME));
            Assert.False(attributeAccessor.HasAttribute(NAME));
        }

        [Fact]
        public void AttributeNames()
        {
            attributeAccessor.SetAttribute(NAME, VALUE);
            attributeAccessor.SetAttribute("abc", "123");
            var attributeNames = attributeAccessor.AttributeNames;
            Assert.Contains(NAME, attributeNames);
            Assert.Contains("abc", attributeNames);
        }

        private class SimpleAttributeAccessor : AbstractAttributeAccessor
        {
        }
    }
}
