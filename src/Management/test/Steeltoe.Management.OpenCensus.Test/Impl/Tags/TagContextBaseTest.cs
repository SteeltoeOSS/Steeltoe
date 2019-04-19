using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Tags.Test
{
    public class TagContextBaseTest
    {
        private static readonly ITag TAG1 = Tag.Create(TagKey.Create("key"), TagValue.Create("val"));
        private static readonly ITag TAG2 = Tag.Create(TagKey.Create("key2"), TagValue.Create("val"));

        [Fact]
        public void Equals_IgnoresTagOrderAndTagContextClass()
        {
  
            var ctx1 = new SimpleTagContext(TAG1, TAG2);
            var ctx2 = new SimpleTagContext(TAG1, TAG2);
            var ctx3 = new SimpleTagContext(TAG2, TAG1);
            var ctx4 = new TestTagContext();
            Assert.True(ctx1.Equals(ctx2));
            Assert.True(ctx1.Equals(ctx3));
            Assert.True(ctx1.Equals(ctx4));
            Assert.True(ctx2.Equals(ctx3));
            Assert.True(ctx2.Equals(ctx4));
            Assert.True(ctx3.Equals(ctx4));

        }

        [Fact]
        public void Equals_HandlesNullIterator()
        {

            var ctx1 = new SimpleTagContext((IList<ITag>)null);
            var ctx2 = new SimpleTagContext((IList<ITag>)null);
            var ctx3 = new SimpleTagContext();
            Assert.True(ctx1.Equals(ctx2));
            Assert.True(ctx1.Equals(ctx3));
            Assert.True(ctx2.Equals(ctx3));
        }

        [Fact]
        public void Equals_DoesNotIgnoreNullTags()
        {

            var ctx1 = new SimpleTagContext(TAG1);
            var ctx2 = new SimpleTagContext(TAG1, null);
            var ctx3 = new SimpleTagContext(null, TAG1);
            var ctx4 = new SimpleTagContext(TAG1, null, null);
            Assert.True(ctx2.Equals(ctx3));
            Assert.False(ctx1.Equals(ctx2));
            Assert.False(ctx1.Equals(ctx3));
            Assert.False(ctx1.Equals(ctx4));
            Assert.False(ctx2.Equals(ctx4));
            Assert.False(ctx3.Equals(ctx4));

        }

        [Fact]
        public void Equals_DoesNotIgnoreDuplicateTags()
        {

            var ctx1 = new SimpleTagContext(TAG1);
            var ctx2 = new SimpleTagContext(TAG1, TAG1);
            Assert.True(ctx1.Equals(ctx1));
            Assert.True(ctx2.Equals(ctx2));
            Assert.False(ctx1.Equals(ctx2));

        }

        [Fact]
        public void TestToString()
        {
            Assert.Equal("TagContext", new SimpleTagContext().ToString());
            Assert.Equal("TagContext", new SimpleTagContext(TAG1, TAG2).ToString());
        }
        class TestTagContext : TagContextBase
        {
            public override IEnumerator<ITag> GetEnumerator()
            {
                var l= new List<ITag>() { TAG1, TAG2 };
                return l.GetEnumerator();
            }
        }

        class SimpleTagContext : TagContextBase
        {
            private readonly IList<ITag> tags;

            // This Error Prone warning doesn't seem correct, because the constructor is just calling
            // another constructor.
            public SimpleTagContext(params ITag[] tags)
                : this(new List<ITag>(tags))
            {
            }

            public SimpleTagContext(IList<ITag> tags)
            {
                this.tags = tags == null ? null : new List<ITag>(tags).AsReadOnly();
            }

            public override IEnumerator<ITag> GetEnumerator()
            {
                return tags == null ? null : tags.GetEnumerator();
            }


        }
    }
}
