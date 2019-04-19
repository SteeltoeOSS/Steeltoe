using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Propagation.Test
{
    public class BinaryFormatBaseTest
    {
        private static readonly IBinaryFormat binaryFormat = BinaryFormatBase.NoopBinaryFormat;

        [Fact]
        public void ToByteArray_NullSpanContext()
        {
            Assert.Throws<ArgumentNullException>(() => binaryFormat.ToByteArray(null));
        }

        [Fact]
        public void ToByteArray_NotNullSpanContext()
        {
            Assert.Equal(new byte[0], binaryFormat.ToByteArray(SpanContext.INVALID));
        }

        [Fact]
        public void FromByteArray_NullInput()
        {
            Assert.Throws<ArgumentNullException>(() => binaryFormat.FromByteArray(null));
        }

        [Fact]
        public void FromByteArray_NotNullInput()
        {
            Assert.Equal(SpanContext.INVALID, binaryFormat.FromByteArray(new byte[0]));
        }
    }
}

