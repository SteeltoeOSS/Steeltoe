// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
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
            var s = new MemoryStream();
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
            Assert.Equal(100, result.F1);
            Assert.Equal(200, result.F2);
        }

        private class Test
        {
            public int F1 = 0;
            public long F2 = 0;
        }
    }
}
