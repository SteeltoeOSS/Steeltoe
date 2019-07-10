// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Moq;
using Steeltoe.Management.Census.Common;
using System;
using Xunit;

namespace Steeltoe.Management.Census.Internal.Test
{
    [Obsolete]
    public class TimestampConverterTest
    {
        private readonly ITimestamp timestamp = Timestamp.Create(1234, 5678);

        private readonly Mock<IClock> mockClock;

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
