using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Propagation.Test
{
    public class SpanContextParseExceptionTest
    {
        [Fact]
        public void CreateWithMessage()
        {
            Assert.Equal("my message", new SpanContextParseException("my message").Message);
        }

        [Fact]
        public void createWithMessageAndCause()
        {
            Exception cause = new Exception();
            SpanContextParseException parseException = new SpanContextParseException("my message", cause);
            Assert.Equal("my message", parseException.Message);
            Assert.Equal(cause, parseException.InnerException);
        }
    }
}
