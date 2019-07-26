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
    public class TagKeyTest
    {
        [Fact]
        public void TestMaxLength()
        {
            Assert.Equal(255, TagKey.MAX_LENGTH);
        }

        [Fact]
        public void TestGetName()
        {
            Assert.Equal("foo", TagKey.Create("foo").Name);
        }

        [Fact]
        public void Create_AllowTagKeyNameWithMaxLength()
        {
            char[] chars = new char[TagKey.MAX_LENGTH];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = 'k';
            }

            string key = new string(chars);
            Assert.Equal(key, TagKey.Create(key).Name);
        }

        [Fact]
        public void Create_DisallowTagKeyNameOverMaxLength()
        {
            char[] chars = new char[TagKey.MAX_LENGTH + 1];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = 'k';
            }

            string key = new string(chars);
            Assert.Throws<ArgumentOutOfRangeException>(() => TagKey.Create(key));
        }

        [Fact]
        public void Create_DisallowUnprintableChars()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TagKey.Create("\u02ab\u03cd"));
        }

        [Fact]
        public void CreateString_DisallowEmpty()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TagKey.Create(string.Empty));
        }

        [Fact]
        public void TestTagKeyEquals()
        {
            var key1 = TagKey.Create("foo");
            var key2 = TagKey.Create("foo");
            var key3 = TagKey.Create("bar");
            Assert.Equal(key1, key2);
            Assert.NotEqual(key3, key1);
            Assert.NotEqual(key3, key2);
        }
    }
}
