//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

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
