using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Stats.Aggregations;
using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Census.Testing.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Stats.Test
{
    public class MeasureToViewMapTest
    {
        private static readonly IMeasure MEASURE = MeasureDouble.Create("my measurement", "measurement description", "By");

        private static readonly IViewName VIEW_NAME = ViewName.Create("my view");

        //private static readonly Cumulative CUMULATIVE = Cumulative.create();

        private static readonly IView VIEW =
            View.Create(
                VIEW_NAME,
                "view description",
                MEASURE,
                Mean.Create(),
                new List<ITagKey>() { TagKey.Create("my key") });

        [Fact]
        public void TestRegisterAndGetView()
        {
            MeasureToViewMap measureToViewMap = new MeasureToViewMap();
            TestClock clock = TestClock.Create(Timestamp.Create(10, 20));
            measureToViewMap.RegisterView(VIEW, clock);
            clock.Time = Timestamp.Create(30, 40);
            IViewData viewData = measureToViewMap.GetView(VIEW_NAME, clock, StatsCollectionState.ENABLED);
            Assert.Equal(VIEW, viewData.View);
            Assert.Empty(viewData.AggregationMap);
        }
    }
}
