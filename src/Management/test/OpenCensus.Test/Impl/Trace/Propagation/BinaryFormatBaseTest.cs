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

namespace Steeltoe.Management.Census.Trace.Propagation.Test
{
    [Obsolete]
    public class BinaryFormatBaseTest
    {
        private static readonly IBinaryFormat BinaryFormat = BinaryFormatBase.NoopBinaryFormat;

        [Fact]
        public void ToByteArray_NullSpanContext()
        {
            Assert.Throws<ArgumentNullException>(() => BinaryFormat.ToByteArray(null));
        }

        [Fact]
        public void ToByteArray_NotNullSpanContext()
        {
            Assert.Equal(new byte[0], BinaryFormat.ToByteArray(SpanContext.INVALID));
        }

        [Fact]
        public void FromByteArray_NullInput()
        {
            Assert.Throws<ArgumentNullException>(() => BinaryFormat.FromByteArray(null));
        }

        [Fact]
        public void FromByteArray_NotNullInput()
        {
            Assert.Equal(SpanContext.INVALID, BinaryFormat.FromByteArray(new byte[0]));
        }
    }
}
