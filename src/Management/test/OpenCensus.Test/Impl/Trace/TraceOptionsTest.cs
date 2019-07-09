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

using System;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Test
{
    [Obsolete]
    public class TraceOptionsTest
    {
        private static readonly byte[] FirstBytes = { (byte)0xff };
        private static readonly byte[] SecondBytes = { 1 };
        private static readonly byte[] ThirdBytes = { 6 };

        [Fact]
        public void GetOptions()
        {
            Assert.Equal(0, TraceOptions.DEFAULT.Options);
            Assert.Equal(0, TraceOptions.Builder().SetIsSampled(false).Build().Options);
            Assert.Equal(1, TraceOptions.Builder().SetIsSampled(true).Build().Options);
            Assert.Equal(0, TraceOptions.Builder().SetIsSampled(true).SetIsSampled(false).Build().Options);
            Assert.Equal(-1, TraceOptions.FromBytes(FirstBytes).Options);
            Assert.Equal(1, TraceOptions.FromBytes(SecondBytes).Options);
            Assert.Equal(6, TraceOptions.FromBytes(ThirdBytes).Options);
        }

        [Fact]
        public void IsSampled()
        {
            Assert.False(TraceOptions.DEFAULT.IsSampled);
            Assert.True(TraceOptions.Builder().SetIsSampled(true).Build().IsSampled);
        }

        [Fact]
        public void ToFromBytes()
        {
            Assert.Equal(FirstBytes, TraceOptions.FromBytes(FirstBytes).Bytes);
            Assert.Equal(SecondBytes, TraceOptions.FromBytes(SecondBytes).Bytes);
            Assert.Equal(ThirdBytes, TraceOptions.FromBytes(ThirdBytes).Bytes);
        }

        [Fact]
        public void Builder_FromOptions()
        {
            Assert.Equal(
                6 | 1,
                    TraceOptions.Builder(TraceOptions.FromBytes(ThirdBytes)).SetIsSampled(true).Build().Options);
        }

        ////[Fact]
        ////public void traceOptions_EqualsAndHashCode()
        ////{
        //    // EqualsTester tester = new EqualsTester();
        //    // tester.addEqualityGroup(TraceOptions.DEFAULT);
        //    // tester.addEqualityGroup(
        //    //    TraceOptions.FromBytes(secondBytes), TraceOptions.Builder().SetIsSampled(true).build());
        //    // tester.addEqualityGroup(TraceOptions.FromBytes(firstBytes));
        //    // tester.testEquals();
        ////}

        [Fact]
        public void TraceOptions_ToString()
        {
            Assert.Contains("sampled=False", TraceOptions.DEFAULT.ToString());
            Assert.Contains("sampled=True", TraceOptions.Builder().SetIsSampled(true).Build().ToString());
        }
    }
}
