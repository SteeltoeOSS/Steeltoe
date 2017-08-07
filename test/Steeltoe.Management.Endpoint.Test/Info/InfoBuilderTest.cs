using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class InfoBuilderTest : BaseTest
    {
        [Fact]
        public void ReturnsEmptyDictionary()
        {
            var builder = new InfoBuilder();
            var built = builder.Build();
            Assert.NotNull(built);
            Assert.Equal(0, built.Count);
        }

        [Fact]
        public void WithInfoSingleValueAddsValue()
        {
            var builder = new InfoBuilder();
            builder.WithInfo("foo", "bar");
            var built = builder.Build();
            Assert.NotNull(built);
            Assert.Equal(1, built.Count);
            Assert.Equal("bar", built["foo"]);
        }

        [Fact]
        public void WithInfoDictionaryAddsValues()
        {
            var builder = new InfoBuilder();
            Dictionary<string, object> items = new Dictionary<string, object>()
            {
                { "foo", "bar" },
                { "bar", 100 }
            };
            builder.WithInfo(items);
            var built = builder.Build();
            Assert.NotNull(built);
            Assert.Equal(2, built.Count);
            Assert.Equal("bar", built["foo"]);
            Assert.Equal(100, built["bar"]);
        }
    }
}
