// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Connector.Services.Test
{
    public class TagsTest
    {
        private static readonly Tags EmptyTags = new ();

        [Fact]
        public void ContainsOne()
        {
            var tags = new Tags(new[] { "test1", "test2" });
            Assert.True(tags.ContainsOne(new[] { "test1", "testx" }));
            Assert.True(tags.ContainsOne(new[] { "testx", "test2" }));
            Assert.False(tags.ContainsOne(new[] { "testx", "testy" }));
        }

        [Fact]
        public void ContainsOne_WithEmptyTags()
        {
            Assert.False(EmptyTags.ContainsOne(new[] { "test" }));
        }

        [Fact]
        public void Contains()
        {
            var tags = new Tags(new[] { "test1", "test2" });
            Assert.True(tags.Contains("test1"));
            Assert.True(tags.Contains("test2"));
            Assert.False(tags.Contains("testx"));
        }

        [Fact]
        public void Contains_WithEmptyTags()
        {
            Assert.False(EmptyTags.Contains("test"));
        }

        [Fact]
        public void StartsWith()
        {
            var tags = new Tags("test");
            Assert.True(tags.StartsWith("test-123"));
            Assert.False(tags.StartsWith("abcd"));
        }

        [Fact]
        public void StartsWith_WithEmptyTags()
        {
            Assert.False(EmptyTags.StartsWith("test"));
        }
    }
}
