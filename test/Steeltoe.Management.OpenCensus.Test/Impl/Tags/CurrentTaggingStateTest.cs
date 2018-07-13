using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Tags.Test
{
    public class CurrentTaggingStateTest
    {
        [Fact]
        public void DefaultState()
        {
            Assert.Equal(TaggingState.ENABLED, new CurrentTaggingState().Value);
        }

        [Fact]
        public void SetState()
        {
            CurrentTaggingState state = new CurrentTaggingState();
            state.Set(TaggingState.DISABLED);
            Assert.Equal(TaggingState.DISABLED, state.Internal);
            state.Set(TaggingState.ENABLED);
            Assert.Equal(TaggingState.ENABLED, state.Internal);
        }


        [Fact]
        public void PreventSettingStateAfterReadingState()
        {
            CurrentTaggingState state = new CurrentTaggingState();
            var current = state.Value;
            Assert.Throws<InvalidOperationException>(() => state.Set(TaggingState.DISABLED));
        }
    }
}
