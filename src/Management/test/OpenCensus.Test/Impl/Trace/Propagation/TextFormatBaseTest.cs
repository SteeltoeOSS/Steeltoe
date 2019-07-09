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
    public class TextFormatBaseTest
    {
        private static readonly ITextFormat TextFormat = TextFormatBase.NoopTextFormat;

        [Fact]
        public void Inject_NullSpanContext()
        {
            Assert.Throws<ArgumentNullException>(() => TextFormat.Inject(null, new object(), new TestSetter()));
        }

        [Fact]
        public void Inject_NotNullSpanContext_DoesNotFail()
        {
            TextFormat.Inject(SpanContext.INVALID, new object(), new TestSetter());
        }

        [Fact]
        public void FromHeaders_NullGetter()
        {
            Assert.Throws<ArgumentNullException>(() => TextFormat.Extract(new object(), null));
        }

        [Fact]
        public void FromHeaders_NotNullGetter()
        {
            Assert.Same(SpanContext.INVALID, TextFormat.Extract(new object(), new TestGetter()));
        }

        private class TestSetter : ISetter<object>
        {
            public void Put(object carrier, string key, string value)
            {
            }
        }

        private class TestGetter : IGetter<object>
        {
            public string Get(object carrier, string key)
            {
                return null;
            }
        }
    }
}
