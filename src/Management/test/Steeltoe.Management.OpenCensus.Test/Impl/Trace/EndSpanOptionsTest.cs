using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class EndSpanOptionsTest
    {
        [Fact]
        public void EndSpanOptions_DefaultOptions()
        {
            Assert.Null(EndSpanOptions.DEFAULT.Status);
            Assert.False(EndSpanOptions.DEFAULT.SampleToLocalSpanStore);
        }

        [Fact]
        public void SetStatus_Ok()
        {
            EndSpanOptions endSpanOptions = EndSpanOptions.Builder().SetStatus(Status.OK).Build();
            Assert.Equal(Status.OK, endSpanOptions.Status);
        }

        [Fact]
        public void SetStatus_Error()
        {
            EndSpanOptions endSpanOptions =
                EndSpanOptions.Builder()
                    .SetStatus(Status.CANCELLED.WithDescription("ThisIsAnError"))
                    .Build();
            Assert.Equal(Status.CANCELLED.WithDescription("ThisIsAnError"), endSpanOptions.Status);
        }

        [Fact]
        public void SetSampleToLocalSpanStore()
        {
            EndSpanOptions endSpanOptions =
                EndSpanOptions.Builder().SetSampleToLocalSpanStore(true).Build();
            Assert.True(endSpanOptions.SampleToLocalSpanStore);
        }

        [Fact]
        public void EndSpanOptions_EqualsAndHashCode()
        {
            //EqualsTester tester = new EqualsTester();
            //tester.addEqualityGroup(
            //    EndSpanOptions.builder()
            //        .setStatus(Status.CANCELLED.withDescription("ThisIsAnError"))
            //        .build(),
            //    EndSpanOptions.builder()
            //        .setStatus(Status.CANCELLED.withDescription("ThisIsAnError"))
            //        .build());
            //tester.addEqualityGroup(EndSpanOptions.builder().build(), EndSpanOptions.DEFAULT);
            //tester.testEquals();
        }

        [Fact]
        public void EndSpanOptions_ToString()
        {
            EndSpanOptions endSpanOptions =
                EndSpanOptions.Builder()
                    .SetStatus(Status.CANCELLED.WithDescription("ThisIsAnError"))
                    .Build();
            Assert.Contains("ThisIsAnError", endSpanOptions.ToString());
        }
    }
}
