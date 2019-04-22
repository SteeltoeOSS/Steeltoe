using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Tags.Propagation.Test
{
    public class TagContextBinarySerializerTest
    {
        private readonly TagsComponent tagsComponent = new TagsComponent();
        private readonly ITagContextBinarySerializer serializer;

        private readonly ITagContext tagContext = new TestTagContext();
        //new TagContext()
        //  {
        //      @Override
        //  public Iterator<Tag> getIterator()
        //      {
        //          return ImmutableSet.< Tag > of(Tag.create(TagKey.create("key"), TagValue.create("value")))
        //              .iterator();
        //      }
        //  };

        public TagContextBinarySerializerTest()
        {
            serializer = tagsComponent.TagPropagationComponent.BinarySerializer;
        }
        //[Fact]
        //public void ToByteArray_TaggingDisabled()
        //{
        //    tagsComponent.setState(TaggingState.DISABLED);
        //    assertThat(serializer.toByteArray(tagContext)).isEmpty();
        //}

        //[Fact]
        //public void ToByteArray_TaggingReenabled()
        //{
        //    byte[] serialized = serializer.ToByteArray(tagContext);
        //    tagsComponent.setState(TaggingState.DISABLED);
        //    assertThat(serializer.toByteArray(tagContext)).isEmpty();
        //    tagsComponent.setState(TaggingState.ENABLED);
        //    assertThat(serializer.toByteArray(tagContext)).isEqualTo(serialized);
        //}

        //[Fact]
        //public void FromByteArray_TaggingDisabled()
        //{
        //    byte[] serialized = serializer.toByteArray(tagContext);
        //    tagsComponent.setState(TaggingState.DISABLED);
        //    assertThat(TagsTestUtil.tagContextToList(serializer.fromByteArray(serialized))).isEmpty();
        //}

        //[Fact]
        //public void FromByteArray_TaggingReenabled()
        //{
        //    byte[] serialized = serializer.toByteArray(tagContext);
        //    tagsComponent.setState(TaggingState.DISABLED);
        //    assertThat(TagsTestUtil.tagContextToList(serializer.fromByteArray(serialized))).isEmpty();
        //    tagsComponent.setState(TaggingState.ENABLED);
        //    assertThat(serializer.fromByteArray(serialized)).isEqualTo(tagContext);
        //}
        class TestTagContext : TagContextBase
        {
            public TestTagContext()
            {

            }
            public override IEnumerator<ITag> GetEnumerator()
            {
                return new List<ITag>() { Tag.Create(TagKey.Create("key"), TagValue.Create("value")) }.GetEnumerator();
            }
        }
    }
}
