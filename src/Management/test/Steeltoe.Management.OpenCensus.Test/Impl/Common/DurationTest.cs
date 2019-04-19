using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Common.Test
{
    public class DurationTest
    {
        [Fact]
        public void TestDurationCreate()
        {
            Assert.Equal(24, Duration.Create(24, 42).Seconds);
            Assert.Equal(42, Duration.Create(24, 42).Nanos);
            Assert.Equal(-24, Duration.Create(-24, -42).Seconds);
            Assert.Equal(-42, Duration.Create(-24, -42).Nanos);
            Assert.Equal(315576000000L, Duration.Create(315576000000L, 999999999).Seconds);
            Assert.Equal(999999999, Duration.Create(315576000000L, 999999999).Nanos);
            Assert.Equal(-315576000000L, Duration.Create(-315576000000L, -999999999).Seconds);
            Assert.Equal(-999999999, Duration.Create(-315576000000L, -999999999).Nanos);
        }
        [Fact]
        public void TestDurationCreateInvalidInput()
        {
            Assert.Equal(Duration.Create(0, 0), Duration.Create(-315576000001L, 0));
            Assert.Equal(Duration.Create(0, 0), Duration.Create(315576000001L, 0));
            Assert.Equal(Duration.Create(0, 0), Duration.Create(0, 1000000000));
            Assert.Equal(Duration.Create(0, 0), Duration.Create(0, -1000000000));
            Assert.Equal(Duration.Create(0, 0), Duration.Create(-1, 1));
            Assert.Equal(Duration.Create(0, 0), Duration.Create(1, -1));
        }

        [Fact]
        public void Duration_CompareLength()
        {
            Assert.Equal(0, Duration.Create(0, 0).CompareTo(Duration.Create(0, 0)));
            Assert.Equal(0, Duration.Create(24, 42).CompareTo(Duration.Create(24, 42)));
            Assert.Equal(0, Duration.Create(-24, -42).CompareTo(Duration.Create(-24, -42)));
            Assert.Equal(1, Duration.Create(25, 42).CompareTo(Duration.Create(24, 42)));
            Assert.Equal(1, Duration.Create(24, 45).CompareTo(Duration.Create(24, 42)));
            Assert.Equal(-1,Duration.Create(24, 42).CompareTo(Duration.Create(25, 42)));
            Assert.Equal(-1,Duration.Create(24, 42).CompareTo(Duration.Create(24, 45)));
            Assert.Equal(-1, Duration.Create(-24, -45).CompareTo(Duration.Create(-24, -42)));
            Assert.Equal(1, Duration.Create(-24, -42).CompareTo(Duration.Create(-25, -42)));
            Assert.Equal(1, Duration.Create(24, 42).CompareTo(Duration.Create(-24, -42)));
        }

        [Fact]
        public void TestDurationEqual()
        {
            // Positive tests.
            Assert.Equal(Duration.Create(0, 0), Duration.Create(0, 0));
            Assert.Equal(Duration.Create(24, 42), Duration.Create(24, 42));
            Assert.Equal(Duration.Create(-24, -42), Duration.Create(-24, -42));
            // Negative tests.
            Assert.NotEqual(Duration.Create(24, 42), Duration.Create(25, 42));
            Assert.NotEqual(Duration.Create(24, 42), Duration.Create(24, 43));
            Assert.NotEqual(Duration.Create(-24, -42), Duration.Create(-25, -42));
            Assert.NotEqual(Duration.Create(-24, -42), Duration.Create(-24, -43));
        }
    }
}
