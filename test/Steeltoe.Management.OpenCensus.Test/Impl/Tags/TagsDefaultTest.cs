using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Tags.Test
{
    public class TagsDefaultTest
    {
        [Fact]
        public void TestState()
        {
            // Test that setState ignores its input.
            //Tags.setState(TaggingState.ENABLED);
            Assert.Equal(TaggingState.DISABLED, Tags.State);

            // Test that setState cannot be called after getState.
            //thrown.expect(IllegalStateException);
            //thrown.expectMessage("State was already read, cannot set state.");
            //Tags.setState(TaggingState.ENABLED);
        }

        [Fact]
        public void DefaultTagger()
        {
            Assert.Equal(NoopTags.NoopTagger, Tags.Tagger);
        }

        [Fact]
        public void DefaultTagContextSerializer()
        {
            Assert.Equal(NoopTags.NoopTagPropagationComponent, Tags.TagPropagationComponent);
        }

    }
}
