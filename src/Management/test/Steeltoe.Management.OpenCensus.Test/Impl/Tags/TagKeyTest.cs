using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Tags.Test
{
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
            for (int i = 0; i < chars.Length; i++) chars[i] = 'k';
            String key = new String(chars);
            Assert.Equal(key, TagKey.Create(key).Name);
        }

        [Fact]
        public void Create_DisallowTagKeyNameOverMaxLength()
        {
            char[] chars = new char[TagKey.MAX_LENGTH + 1];
            for (int i = 0; i < chars.Length; i++) chars[i] = 'k';
            String key = new String(chars);
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
            Assert.Throws<ArgumentOutOfRangeException>(() => TagKey.Create(""));
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
