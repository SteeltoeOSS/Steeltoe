using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Tags.Propagation.Test
{
    public class TagContextSerializationExceptionTest
    {
        [Fact]
        public void CreateWithMessage()
        {
            Assert.Equal("my message", new TagContextSerializationException("my message").Message);
        }

        [Fact]
        public void CreateWithMessageAndCause()
        {
            Exception cause = new Exception();
            TagContextSerializationException exception = new TagContextSerializationException("my message", cause);
            Assert.Equal("my message", exception.Message);
            Assert.Equal(cause, exception.InnerException);
        }
    }
}
