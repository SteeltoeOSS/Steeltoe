using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Testing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Stats.Test
{
    public class StatsComponentTest
    {
        private readonly StatsComponent statsComponent = new StatsComponent(new SimpleEventQueue(), TestClock.Create());

        [Fact]
        public void DefaultState()
        {
            Assert.Equal(StatsCollectionState.ENABLED, statsComponent.State);
        }

        //[Fact]
        //public void setState_Disabled()
        //{
        //    statsComponent.setState(StatsCollectionState.DISABLED);
        //    assertThat(statsComponent.getState()).isEqualTo(StatsCollectionState.DISABLED);
        //}

        //[Fact]
        //public void setState_Enabled()
        //{
        //    statsComponent.setState(StatsCollectionState.DISABLED);
        //    statsComponent.setState(StatsCollectionState.ENABLED);
        //    assertThat(statsComponent.getState()).isEqualTo(StatsCollectionState.ENABLED);
        //}

        //[Fact]
        //public void setState_DisallowsNull()
        //{
        //    thrown.expect(NullPointerException);
        //    thrown.expectMessage("newState");
        //    statsComponent.setState(null);
        //}

        //[Fact]
        //public void preventSettingStateAfterGettingState()
        //{
        //    statsComponent.setState(StatsCollectionState.DISABLED);
        //    statsComponent.getState();
        //    thrown.expect(IllegalStateException);
        //    thrown.expectMessage("State was already read, cannot set state.");
        //    statsComponent.setState(StatsCollectionState.ENABLED);
        //}
    }
}
