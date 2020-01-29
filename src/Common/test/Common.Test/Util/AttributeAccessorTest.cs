// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            var attributeNames = this.attributeAccessor.AttributeNames;
            Assert.Contains(NAME, attributeNames);
            Assert.Contains("abc", attributeNames);
        }

        private class SimpleAttributeAccessor : AbstractAttributeAccessor
        {
        }
    }
}
