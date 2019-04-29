using Steeltoe.Management.Census.Common;
using Xunit;
using Moq;

namespace Steeltoe.Management.Census.Internal.Test
{
    public class TimestampConverterTest
    {
        private readonly ITimestamp timestamp = Timestamp.Create(1234, 5678);

        private Mock<IClock> mockClock;

        public TimestampConverterTest()
        {
            mockClock = new Mock<IClock>();
        }

        [Fact]
        public void ConvertNanoTime()
        {
            mockClock.Setup(clock => clock.Now).Returns(timestamp);
            mockClock.Setup(clock => clock.NowNanos).Returns(1234L);

            ITimestampConverter timeConverter = TimestampConverter.Now(mockClock.Object);
            Assert.Equal(Timestamp.Create(1234, 10678), timeConverter.ConvertNanoTime(6234));
            Assert.Equal(Timestamp.Create(1234, 5444), timeConverter.ConvertNanoTime(1000));
            Assert.Equal(Timestamp.Create(1235, 0), timeConverter.ConvertNanoTime(999995556));
        }
    } 
}
