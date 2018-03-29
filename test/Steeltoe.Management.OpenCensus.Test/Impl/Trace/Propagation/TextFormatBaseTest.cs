using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Census.Trace.Propagation.Test
{
    public class TextFormatTest
    {
        private static readonly ITextFormat textFormat = TextFormatBase.NoopTextFormat;

        [Fact]
        public void Inject_NullSpanContext()
        {
            Assert.Throws<ArgumentNullException>(() => textFormat.Inject(null, new object(), new TestSetter()));
        }

        [Fact]
        public void Inject_NotNullSpanContext_DoesNotFail()
        {
            textFormat.Inject(SpanContext.INVALID, new object(), new TestSetter());
        }

        [Fact]
        public void FromHeaders_NullGetter()
        {
            Assert.Throws<ArgumentNullException>(() => textFormat.Extract(new object(), null));
        }

        [Fact]
        public void FromHeaders_NotNullGetter()
        {
            Assert.Same(SpanContext.INVALID, textFormat.Extract(new object(), new TestGetter()));
        }
        class TestSetter : ISetter<object>
        {
            public void Put(object carrier, string key, string value)
            {
            }
        }
        class TestGetter : IGetter<object>
        {
            public string Get(object carrier, string key)
            {
                return null;
            }
        }
    }
}
