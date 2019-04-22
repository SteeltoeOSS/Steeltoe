using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Tags.Test
{
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
            for(int i = 0; i < chars.Length; i++)  chars[i] = 'v';
            String value = new String(chars);
            Assert.Equal(value, TagValue.Create(value).AsString);
        }

        [Fact]
        public void Create_DisallowTagValueOverMaxLength()
        {
            char[] chars = new char[TagValue.MAX_LENGTH + 1];
            for (int i = 0; i < chars.Length; i++) chars[i] = 'v';
            String value = new String(chars);
            Assert.Throws<ArgumentOutOfRangeException>(() => TagValue.Create(value));
        }

        [Fact]
        public void DisallowTagValueWithUnprintableChars()
        {
            String value = "\u02ab\u03cd";
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
