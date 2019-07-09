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

using System;
using Xunit;

namespace Steeltoe.Management.Census.Tags.Test
{
    [Obsolete]
    public class TagValueTest
    {
        [Fact]
        public void TestMaxLength()
        {
            Assert.Equal(255, TagValue.MAX_LENGTH);
        }

        [Fact]
        public void TestAsString()
        {
            Assert.Equal("foo", TagValue.Create("foo").AsString);
        }

        [Fact]
        public void Create_AllowTagValueWithMaxLength()
        {
            char[] chars = new char[TagValue.MAX_LENGTH];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = 'v';
            }

            string value = new string(chars);
            Assert.Equal(value, TagValue.Create(value).AsString);
        }

        [Fact]
        public void Create_DisallowTagValueOverMaxLength()
        {
            char[] chars = new char[TagValue.MAX_LENGTH + 1];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = 'v';
            }

            string value = new string(chars);
            Assert.Throws<ArgumentOutOfRangeException>(() => TagValue.Create(value));
        }

        [Fact]
        public void DisallowTagValueWithUnprintableChars()
        {
            string value = "\u02ab\u03cd";
            Assert.Throws<ArgumentOutOfRangeException>(() => TagValue.Create(value));
        }

        [Fact]
        public void TestTagValueEquals()
        {
            var v1 = TagValue.Create("foo");
            var v2 = TagValue.Create("foo");
            var v3 = TagValue.Create("bar");
            Assert.Equal(v1, v2);
            Assert.NotEqual(v1, v3);
            Assert.NotEqual(v2, v3);
            Assert.Equal(v3, v3);
        }
    }
}
