using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Http.Test
{
    public class SerializationHelperTest
    {
        [Fact]
        public void Deserialize_ThrowsNulls()
        {
            Assert.Throws<ArgumentNullException>(() => SerializationHelper.Deserialize<Test>(null, null));
        }

        [Fact]
        public void Deserialize_ReturnsNullOnException()
        {
            MemoryStream s = new MemoryStream();
            var result = SerializationHelper.Deserialize<Test>(s);
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_ReturnsValid()
        {
            var memStream = new MemoryStream();
            var textWriter = new StreamWriter(memStream);
            textWriter.Write("{\"f1\":100,\"f2\":200}");
            textWriter.Flush();
            memStream.Seek(0, SeekOrigin.Begin);

            var result = SerializationHelper.Deserialize<Test>(memStream);
            Assert.NotNull(result);
            Assert.Equal(100, result.f1);
            Assert.Equal(200, result.f2);
        }
        class Test
        {
            public int f1=0;
            public long f2=0;
        }
    }
}
