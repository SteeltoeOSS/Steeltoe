using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Stats.Test
{
    public class CurrentStatsStateTest
    {
        [Fact]
        public void DefaultState()
        {
            Assert.Equal(StatsCollectionState.ENABLED, new CurrentStatsState().Value);
        }

        [Fact]
        public void SetState()
        {
            CurrentStatsState state = new CurrentStatsState();
            Assert.True(state.Set(StatsCollectionState.DISABLED));
            Assert.Equal(StatsCollectionState.DISABLED, state.Internal);
            Assert.True(state.Set(StatsCollectionState.ENABLED));
            Assert.Equal(StatsCollectionState.ENABLED, state.Internal);
            Assert.False(state.Set(StatsCollectionState.ENABLED));
        }



        [Fact]
        public void PreventSettingStateAfterReadingState()
        {
            CurrentStatsState state = new CurrentStatsState();
            var st = state.Value;
            Assert.Throws<ArgumentException>(() => state.Set(StatsCollectionState.DISABLED));
        }
    }
}
