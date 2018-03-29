using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Stats.Aggregations;
using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Stats.Test
{
    public class ViewDataTest
    {
        // tag keys
        private static readonly ITagKey K1 = TagKey.Create("k1");
        private static readonly ITagKey K2 = TagKey.Create("k2");
        private static readonly IList<ITagKey> TAG_KEYS = new List<ITagKey>() { K1, K2 };

        // tag values
        private static readonly ITagValue V1 = TagValue.Create("v1");
        private static readonly ITagValue V2 = TagValue.Create("v2");
        private static readonly ITagValue V10 = TagValue.Create("v10");
        private static readonly ITagValue V20 = TagValue.Create("v20");

        //private static readonly AggregationWindow CUMULATIVE = Cumulative.Create();
        //private static readonly AggregationWindow INTERVAL_HOUR = Interval.Create(Duration.Create(3600, 0));

        private static readonly IBucketBoundaries BUCKET_BOUNDARIES =
            BucketBoundaries.Create(new List<double>() { 10.0, 20.0, 30.0, 40.0 });

        private static readonly IAggregation DISTRIBUTION = Distribution.Create(BUCKET_BOUNDARIES);

        private static readonly IDictionary<TagValues, IAggregationData> ENTRIES =
            new Dictionary<TagValues, IAggregationData>() {
          { TagValues.Create(new List<ITagValue>(){ V1, V2 }), DistributionData.Create(1, 1, 1, 1, 0, new List<long>() {0L, 1L, 0L }) },
          { TagValues.Create(new List<ITagValue>(){ V10, V20 }), DistributionData.Create(-5, 6, -20, 5, 100.1,  new List<long>() {5L, 0L, 1L }) }
            };
        // name
        private static readonly IViewName NAME = ViewName.Create("test-view");
        // description
        private static readonly String DESCRIPTION = "test-view-descriptor description";
        // measure
        private static readonly IMeasure MEASURE_DOUBLE =
            MeasureDouble.Create("measure1", "measure description", "1");
        private static readonly IMeasure MEASURE_LONG =
            MeasureLong.Create("measure2", "measure description", "1");

        [Fact]
        public void TestCumulativeViewData()
        {
            IView view = View.Create(NAME, DESCRIPTION, MEASURE_DOUBLE, DISTRIBUTION, TAG_KEYS);
            ITimestamp start = Timestamp.FromMillis(1000);
            ITimestamp end = Timestamp.FromMillis(2000);
            IViewData viewData = ViewData.Create(view, ENTRIES, start, end);
            Assert.Equal(view, viewData.View);
            Assert.Equal(ENTRIES, viewData.AggregationMap);
        }

        //[Fact]
        //public void TestIntervalViewData()
        //{
        //    View view =
        //        View.Create(NAME, DESCRIPTION, MEASURE_DOUBLE, DISTRIBUTION, TAG_KEYS, INTERVAL_HOUR);
        //    Timestamp end = Timestamp.fromMillis(2000);
        //    AggregationWindowData windowData = IntervalData.Create(end);
        //    ViewData viewData = ViewData.Create(view, ENTRIES, windowData);
        //    assertThat(viewData.getView()).isEqualTo(view);
        //    assertThat(viewData.getAggregationMap()).isEqualTo(ENTRIES);
        //    assertThat(viewData.getWindowData()).isEqualTo(windowData);
        //}

        [Fact]
        public void TestViewDataEquals()
        {
            IView cumulativeView =
                View.Create(NAME, DESCRIPTION, MEASURE_DOUBLE, DISTRIBUTION, TAG_KEYS);
            //View intervalView =
            //    View.Create(NAME, DESCRIPTION, MEASURE_DOUBLE, DISTRIBUTION, TAG_KEYS, INTERVAL_HOUR);

            //new EqualsTester()
            //    .addEqualityGroup(
            IViewData data1 = ViewData.Create(
                        cumulativeView,
                        ENTRIES,
                        Timestamp.FromMillis(1000), Timestamp.FromMillis(2000));
            IViewData data2 = ViewData.Create(
                        cumulativeView,
                        ENTRIES,
                        Timestamp.FromMillis(1000), Timestamp.FromMillis(2000));
            Assert.Equal(data1, data2);
           
            //.addEqualityGroup(
            IViewData data3 = ViewData.Create(
                        cumulativeView,
                        ENTRIES,
                        Timestamp.FromMillis(1000), Timestamp.FromMillis(3000));
            Assert.NotEqual(data1, data3);
            Assert.NotEqual(data2, data3);

            //.addEqualityGroup(
            //IViewData data4 = ViewData.Create(intervalView, ENTRIES, IntervalData.Create(Timestamp.fromMillis(2000))),
            //IViewData data5 = ViewData.Create(intervalView, ENTRIES, IntervalData.Create(Timestamp.fromMillis(2000))))
                //.addEqualityGroup(
                //    ViewData.Create(
                //        intervalView,
                //        Collections.< List<TagValue>, AggregationData > emptyMap(),
                //        IntervalData.Create(Timestamp.fromMillis(2000))))
                //.testEquals();
        }

        //        [Fact]
        //  public void testAggregationWindowDataMatch()
        //        {
        //            final Timestamp start = Timestamp.fromMillis(1000);
        //            final Timestamp end = Timestamp.fromMillis(2000);
        //            final AggregationWindowData windowData1 = CumulativeData.Create(start, end);
        //            final AggregationWindowData windowData2 = IntervalData.Create(end);
        //            windowData1.match(
        //                new Function<CumulativeData, Void>() {
        //          @Override
        //                  public Void apply(CumulativeData windowData)
        //            {
        //                assertThat(windowData.getStart()).isEqualTo(start);
        //                assertThat(windowData.getEnd()).isEqualTo(end);
        //                return null;
        //            }
        //        },
        //        new Function<IntervalData, Void>() {
        //          @Override
        //          public Void apply(IntervalData windowData)
        //        {
        //            fail("CumulativeData expected.");
        //            return null;
        //        }
        //    },
        //        Functions.<Void>throwIllegalArgumentException());
        //    windowData2.match(
        //        new Function<CumulativeData, Void>() {
        //          @Override
        //          public Void apply(CumulativeData windowData)
        //    {
        //        fail("IntervalData expected.");
        //        return null;
        //    }
        //},
        //        new Function<IntervalData, Void>() {
        //          @Override
        //          public Void apply(IntervalData windowData)
        //{
        //    assertThat(windowData.getEnd()).isEqualTo(end);
        //    return null;
        //}
        //        },
        //        Functions.<Void>throwIllegalArgumentException());
        //  }

        //  [Fact]
        //  public void preventWindowAndAggregationWindowDataMismatch()
        //{
        //    thrown.expect(IllegalArgumentException);
        //    thrown.expectMessage("AggregationWindow and AggregationWindowData types mismatch. ");
        //    ViewData.Create(
        //        View.Create(NAME, DESCRIPTION, MEASURE_DOUBLE, DISTRIBUTION, TAG_KEYS, INTERVAL_HOUR),
        //        ENTRIES,
        //        CumulativeData.Create(Timestamp.fromMillis(1000), Timestamp.fromMillis(2000)));
        //  }

        //  [Fact]
        //  public void preventWindowAndAggregationWindowDataMismatch2()
        //{
        //    thrown.expect(IllegalArgumentException.class);
        //    thrown.expectMessage("AggregationWindow and AggregationWindowData types mismatch. ");
        //    ViewData.Create(
        //        View.Create(NAME, DESCRIPTION, MEASURE_DOUBLE, DISTRIBUTION, TAG_KEYS, CUMULATIVE),
        //        ENTRIES,
        //        IntervalData.Create(Timestamp.fromMillis(1000)));
        //  }

        //  [Fact]
        //  public void preventStartTimeLaterThanEndTime()
        //{
        //    thrown.expect(IllegalArgumentException.class);
        //    CumulativeData.Create(Timestamp.fromMillis(3000), Timestamp.fromMillis(2000));
        //  }

        //[Fact]
        //public void PreventAggregationAndAggregationDataMismatch_SumDouble_SumLong()
        //{
        //    AggregationAndAggregationDataMismatch(
        //        CreateView(Sum.Create(), MEASURE_DOUBLE),
        //        ImmutableMap.< List<TagValue>, AggregationData > of(
        //            Arrays.asList(V1, V2), SumDataLong.Create(100)));
        //}

        //[Fact]
        //  public void preventAggregationAndAggregationDataMismatch_SumLong_SumDouble()
        //{
        //    aggregationAndAggregationDataMismatch(
        //        CreateView(Sum.Create(), MEASURE_LONG),
        //        ImmutableMap.< List<TagValue>, AggregationData > of(
        //            Arrays.asList(V1, V2), SumDataDouble.Create(100)));
        //}

        //[Fact]
        //  public void preventAggregationAndAggregationDataMismatch_Count_Distribution()
        //{
        //    aggregationAndAggregationDataMismatch(CreateView(Count.Create()), ENTRIES);
        //}

        //[Fact]
        //  public void preventAggregationAndAggregationDataMismatch_Mean_Distribution()
        //{
        //    aggregationAndAggregationDataMismatch(CreateView(Mean.Create()), ENTRIES);
        //}

        //[Fact]
        //  public void preventAggregationAndAggregationDataMismatch_Distribution_Count()
        //{
        //    aggregationAndAggregationDataMismatch(
        //        CreateView(DISTRIBUTION),
        //        ImmutableMap.of(
        //            Arrays.asList(V1, V2),
        //            DistributionData.Create(1, 1, 1, 1, 0, Arrays.asList(0L, 1L, 0L)),
        //            Arrays.asList(V10, V20),
        //            CountData.Create(100)));
        //}

        //private static View CreateView(Aggregation aggregation)
        //{
        //    return CreateView(aggregation, MEASURE_DOUBLE);
        //}

        //private static View CreateView(Aggregation aggregation, Measure measure)
        //{
        //    return View.Create(NAME, DESCRIPTION, measure, aggregation, TAG_KEYS, CUMULATIVE);
        //}

        //private void AggregationAndAggregationDataMismatch(IView view, IDictionary<TagValues, IAggregationData> entries)
        //{
        //    //CumulativeData cumulativeData =
        //    //    CumulativeData.Create(Timestamp.fromMillis(1000), Timestamp.fromMillis(2000));
        //    //thrown.expect(IllegalArgumentException);
        //    //thrown.expectMessage("Aggregation and AggregationData types mismatch. ");
        //    ViewData.Create(view, entries, Timestamp.FromMillis(1000), Timestamp.FromMillis(2000));
        //}

    }
}
