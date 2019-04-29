using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    public class StatusTest
    {
        [Fact]
        public void Status_Ok()
        {
            Assert.Equal(CanonicalCode.OK, Status.OK.CanonicalCode);
            Assert.Null(Status.OK.Description);
            Assert.True(Status.OK.IsOk);
        }

        [Fact]
        public void CreateStatus_WithDescription()
        {
            Status status = Status.UNKNOWN.WithDescription("This is an error.");
            Assert.Equal(CanonicalCode.UNKNOWN, status.CanonicalCode);
            Assert.Equal("This is an error.", status.Description);
            Assert.False(status.IsOk);
        }

        [Fact]
        public void Status_EqualsAndHashCode()
        {
            //EqualsTester tester = new EqualsTester();
            //tester.addEqualityGroup(Status.OK, Status.OK.withDescription(null));
            //tester.addEqualityGroup(
            //    Status.CANCELLED.withDescription("ThisIsAnError"),
            //    Status.CANCELLED.withDescription("ThisIsAnError"));
            //tester.addEqualityGroup(Status.UNKNOWN.withDescription("This is an error."));
            //tester.testEquals();
        }
    }
}
