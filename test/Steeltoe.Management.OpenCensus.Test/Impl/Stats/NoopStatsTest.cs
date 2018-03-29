using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Stats.Test
{
    public class NoopStatsTest
    {
        private static readonly ITag TAG = Tag.Create(TagKey.Create("key"), TagValue.Create("value"));
        private static readonly IMeasureDouble MEASURE =  MeasureDouble.Create("my measure", "description", "s");

        private readonly ITagContext tagContext = new TestTagContext();


        [Fact]
        public void NoopStatsComponent()
        {
            Assert.Same(NoopStats.NoopStatsRecorder, NoopStats.NewNoopStatsComponent().StatsRecorder);
            Assert.Equal(NoopStats.NewNoopViewManager().GetType(), NoopStats.NewNoopStatsComponent().ViewManager.GetType());
        }

        [Fact]
        public void NoopStatsComponent_GetState()
        {
            Assert.Equal(StatsCollectionState.DISABLED, NoopStats.NewNoopStatsComponent().State);
        }

        //[Fact]
        //public void NoopStatsComponent_SetState_IgnoresInput()
        //{
        //    IStatsComponent noopStatsComponent = NoopStats.NewNoopStatsComponent();
        //    noopStatsComponent.State = StatsCollectionState.ENABLED;
        //    assertThat(noopStatsComponent.getState()).isEqualTo(StatsCollectionState.DISABLED);
        //}

        //[Fact]
        //public void NoopStatsComponent_SetState_DisallowsNull()
        //{
        //    StatsComponent noopStatsComponent = NoopStats.newNoopStatsComponent();
        //    thrown.expect(NullPointerException);
        //    noopStatsComponent.setState(null);
        //}

        //[Fact]
        //public void NoopStatsComponent_DisallowsSetStateAfterGetState()
        //{
        //    StatsComponent noopStatsComponent = NoopStats.newNoopStatsComponent();
        //    noopStatsComponent.setState(StatsCollectionState.DISABLED);
        //    noopStatsComponent.getState();
        //    thrown.expect(IllegalStateException);
        //    thrown.expectMessage("State was already read, cannot set state.");
        //    noopStatsComponent.setState(StatsCollectionState.ENABLED);
        //}

        // The NoopStatsRecorder should do nothing, so this test just checks that record doesn't throw an
        // exception.
        [Fact]
        public void NoopStatsRecorder_Record()
        {
            NoopStats.NoopStatsRecorder.NewMeasureMap().Put(MEASURE, 5).Record(tagContext);
        }

        // The NoopStatsRecorder should do nothing, so this test just checks that record doesn't throw an
        // exception.
        [Fact]
        public void NoopStatsRecorder_RecordWithCurrentContext()
        {
            NoopStats.NoopStatsRecorder.NewMeasureMap().Put(MEASURE, 6).Record();
        }

        [Fact]
        public void NoopStatsRecorder_Record_DisallowNullTagContext()
        {
            IMeasureMap measureMap = NoopStats.NoopStatsRecorder.NewMeasureMap();
            Assert.Throws<ArgumentNullException>(() => measureMap.Record(null));
        }

        class TestTagContext : ITagContext
        {
            public IEnumerator<ITag> GetEnumerator()
            {
                var l =  new List<ITag>() { TAG };
                return l.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
