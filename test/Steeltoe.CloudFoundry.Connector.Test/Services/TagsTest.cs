//
// Copyright 2015 the original author or authors.
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

using Xunit;

namespace Steeltoe.CloudFoundry.Connector.Services.Test
{
    public class TagsTest
    {
        private static Tags EMPTY_TAGS = new Tags();

        [Fact]
        public void ContainsOne()
        {
            Tags tags = new Tags(new string[] { "test1", "test2" });
            Assert.True(tags.ContainsOne(new string[] { "test1", "testx" }));
            Assert.True(tags.ContainsOne(new string[] { "testx", "test2" }));
            Assert.False(tags.ContainsOne(new string[] { "testx", "testy" }));
        }

        [Fact]
        public void ContainsOne_WithEmptyTags()
        {
            Assert.False(EMPTY_TAGS.ContainsOne(new string[] { "test" }));
        }

        [Fact]
        public void Contains()
        {
            Tags tags = new Tags(new string[] { "test1", "test2" });
            Assert.True(tags.Contains("test1"));
            Assert.True(tags.Contains("test2"));
            Assert.False(tags.Contains("testx"));
        }

        [Fact]
        public void Contains_WithEmptyTags()
        {
            Assert.False(EMPTY_TAGS.Contains("test"));
        }

        [Fact]
        public void StartsWith()
        {
            Tags tags = new Tags("test");
            Assert.True(tags.StartsWith("test-123"));
            Assert.False(tags.StartsWith("abcd"));
        }

        [Fact]
        public void StartsWith_WithEmptyTags()
        {
            Assert.False(EMPTY_TAGS.StartsWith("test"));
        }
    }
}
