using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Tags.Test
{
    public class TagTest
    {
        [Fact]
        public void TestGetKey()
        {
            Assert.Equal(TagKey.Create("k"), Tag.Create(TagKey.Create("k"), TagValue.Create("v")).Key);
        }

        [Fact]
        public void TestTagEquals()
        {
            ITag tag1 = Tag.Create(TagKey.Create("Key"), TagValue.Create("foo"));
            ITag tag2 = Tag.Create(TagKey.Create("Key"), TagValue.Create("foo"));
            ITag tag3 = Tag.Create(TagKey.Create("Key"), TagValue.Create("bar"));
            ITag tag4 = Tag.Create(TagKey.Create("Key2"), TagValue.Create("foo"));
            Assert.Equal(tag1, tag2);
            Assert.NotEqual(tag1, tag3);
            Assert.NotEqual(tag1, tag4);
            Assert.NotEqual(tag2, tag3);
            Assert.NotEqual(tag2, tag4);
            Assert.NotEqual(tag3, tag4);

        }
    }
}
