using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Stats.Aggregations;
using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Census.Tags.Unsafe;
using Steeltoe.Management.Census.Testing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Stats.Test
{
    public class StatsRecorderTest
    {
        private static readonly ITagKey KEY = TagKey.Create("KEY");
        private static readonly ITagValue VALUE = TagValue.Create("VALUE");
        private static readonly ITagValue VALUE_2 = TagValue.Create("VALUE_2");
        private static readonly IMeasureDouble MEASURE_DOUBLE = MeasureDouble.Create("my measurement", "description", "us");
        private static readonly IMeasureDouble MEASURE_DOUBLE_NO_VIEW_1 = MeasureDouble.Create("my measurement no view 1", "description", "us");
        private static readonly IMeasureDouble MEASURE_DOUBLE_NO_VIEW_2 = MeasureDouble.Create("my measurement no view 2", "description", "us");
        private static readonly IViewName VIEW_NAME = ViewName.Create("my view");

        private readonly StatsComponent statsComponent;
        private readonly IViewManager viewManager;
        private readonly IStatsRecorder statsRecorder;

        public StatsRecorderTest()
        {
            statsComponent = new StatsComponent(new SimpleEventQueue(), TestClock.Create());
            viewManager = statsComponent.ViewManager;
            statsRecorder = statsComponent.StatsRecorder;
        }

        [Fact]
        public void Record_CurrentContextNotSet()
        {
            IView view =
                View.Create(
                    VIEW_NAME,
                    "description",
                    MEASURE_DOUBLE,
                    Sum.Create(),
                    new List<ITagKey>() { KEY });
            viewManager.RegisterView(view);
            statsRecorder.NewMeasureMap().Put(MEASURE_DOUBLE, 1.0).Record();
            IViewData viewData = viewManager.GetView(VIEW_NAME);

            // record() should have used the default TagContext, so the tag value should be null.
            ICollection<TagValues> expected = new List<TagValues>() { TagValues.Create(new List<ITagValue>() { null }) };
            Assert.Equal(expected, viewData.AggregationMap.Keys);
          
        }

        [Fact]
        public void record_CurrentContextSet()
        {
            IView view =
                View.Create(
                    VIEW_NAME,
                    "description",
                    MEASURE_DOUBLE,
                    Sum.Create(),
               new List<ITagKey>() { KEY });
            viewManager.RegisterView(view);
            var orig = AsyncLocalContext.CurrentTagContext;
            AsyncLocalContext.CurrentTagContext = new SimpleTagContext(Tag.Create(KEY, VALUE));
 
            try
            {
                statsRecorder.NewMeasureMap().Put(MEASURE_DOUBLE, 1.0).Record();
            }
            finally
            {
                AsyncLocalContext.CurrentTagContext = orig;
            }
            IViewData viewData = viewManager.GetView(VIEW_NAME);

            // record() should have used the given TagContext.
            ICollection<TagValues> expected = new List<TagValues>() { TagValues.Create(new List<ITagValue>() { VALUE }) };
            Assert.Equal(expected, viewData.AggregationMap.Keys);
        }

        [Fact]
        public void Record_UnregisteredMeasure()
        {
            IView view =
                View.Create(
                    VIEW_NAME,
                    "description",
                    MEASURE_DOUBLE,
                    Sum.Create(),
                    new List<ITagKey>() { KEY });
            viewManager.RegisterView(view);
            statsRecorder
                .NewMeasureMap()
                .Put(MEASURE_DOUBLE_NO_VIEW_1, 1.0)
                .Put(MEASURE_DOUBLE, 2.0)
                .Put(MEASURE_DOUBLE_NO_VIEW_2, 3.0)
                .Record(new SimpleTagContext(Tag.Create(KEY, VALUE)));

            IViewData viewData = viewManager.GetView(VIEW_NAME);

            // There should be one entry.
            var tv = TagValues.Create(new List<ITagValue>() { VALUE });
            StatsTestUtil.AssertAggregationMapEquals(
                viewData.AggregationMap,
                new Dictionary<TagValues, IAggregationData>() {
                    { tv, StatsTestUtil.CreateAggregationData(Sum.Create(), MEASURE_DOUBLE, 2.0) }
                },
                1e-6);
        }

        [Fact]
        public void recordTwice()
        {
            IView view =
                View.Create(
                    VIEW_NAME,
                    "description",
                    MEASURE_DOUBLE,
                    Sum.Create(),
                    new List<ITagKey>() { KEY });

            viewManager.RegisterView(view);
            IMeasureMap statsRecord = statsRecorder.NewMeasureMap().Put(MEASURE_DOUBLE, 1.0);
            statsRecord.Record(new SimpleTagContext(Tag.Create(KEY, VALUE)));
            statsRecord.Record(new SimpleTagContext(Tag.Create(KEY, VALUE_2)));
            IViewData viewData = viewManager.GetView(VIEW_NAME);

            // There should be two entries.
            var tv = TagValues.Create(new List<ITagValue>() { VALUE });
            var tv2 = TagValues.Create(new List<ITagValue>() { VALUE_2 });

            StatsTestUtil.AssertAggregationMapEquals(
                viewData.AggregationMap,
                new Dictionary<TagValues, IAggregationData>() {
                    { tv, StatsTestUtil.CreateAggregationData(Sum.Create(), MEASURE_DOUBLE, 1.0) },
                    { tv2, StatsTestUtil.CreateAggregationData(Sum.Create(), MEASURE_DOUBLE, 1.0) }
                },
                1e-6);
        }

        //[Fact]
        //public void Record_StatsDisabled()
        //{
        //    IView view =
        //        View.Create(
        //            VIEW_NAME,
        //            "description",
        //            MEASURE_DOUBLE,
        //            Sum.Create(),
        //      new List<ITagKey>() { KEY });

        //    viewManager.RegisterView(view);
        //    statsComponent.State (StatsCollectionState.DISABLED);
        //    statsRecorder
        //        .newMeasureMap()
        //        .put(MEASURE_DOUBLE, 1.0)
        //        .record(new SimpleTagContext(Tag.Create(KEY, VALUE)));
        //    assertThat(viewManager.getView(VIEW_NAME)).isEqualTo(CreateEmptyViewData(view));
        //}

        //      [Fact]
        //public void record_StatsReenabled()
        //      {
        //          View view =
        //              View.Create(
        //                  VIEW_NAME,
        //                  "description",
        //                  MEASURE_DOUBLE,
        //                  Sum.Create(),
        //                  Arrays.asList(KEY),
        //                  Cumulative.Create());
        //          viewManager.registerView(view);

        //          statsComponent.setState(StatsCollectionState.DISABLED);
        //          statsRecorder
        //              .newMeasureMap()
        //              .put(MEASURE_DOUBLE, 1.0)
        //              .record(new SimpleTagContext(Tag.Create(KEY, VALUE)));
        //          assertThat(viewManager.getView(VIEW_NAME)).isEqualTo(CreateEmptyViewData(view));

        //          statsComponent.setState(StatsCollectionState.ENABLED);
        //          assertThat(viewManager.getView(VIEW_NAME).getAggregationMap()).isEmpty();
        //          assertThat(viewManager.getView(VIEW_NAME).getWindowData())
        //              .isNotEqualTo(CumulativeData.Create(ZERO_TIMESTAMP, ZERO_TIMESTAMP));
        //          statsRecorder
        //              .newMeasureMap()
        //              .put(MEASURE_DOUBLE, 4.0)
        //              .record(new SimpleTagContext(Tag.Create(KEY, VALUE)));
        //          StatsTestUtil.assertAggregationMapEquals(
        //              viewManager.getView(VIEW_NAME).getAggregationMap(),
        //              ImmutableMap.of(
        //                  Arrays.asList(VALUE),
        //                  StatsTestUtil.CreateAggregationData(Sum.Create(), MEASURE_DOUBLE, 4.0)),
        //              1e-6);
        //      }

        class SimpleTagContext : TagContextBase
        {
            private readonly IList<ITag> tags;

            public SimpleTagContext(params ITag[] tags)
            {
                this.tags = new List<ITag>(tags);
            }

            public override IEnumerator<ITag> GetEnumerator()
            {
                return tags.GetEnumerator();
            }

        }
    }
}
