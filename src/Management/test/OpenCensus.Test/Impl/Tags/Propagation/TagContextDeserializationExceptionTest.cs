using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Tags.Propagation.Test
{
    public class TagContextDeserializationExceptionTest
    {
        [Fact]
        public void CreateWithMessage()
        {
            Assert.Equal("my message", new TagContextDeserializationException("my message").Message);
        }

        [Fact]
        public void CreateWithMessageAndCause()
        {
            Exception cause = new Exception();
            TagContextDeserializationException exception = new TagContextDeserializationException("my message", cause);
            Assert.Equal("my message", exception.Message);
            Assert.Equal(cause, exception.InnerException);
        }
    }
}
