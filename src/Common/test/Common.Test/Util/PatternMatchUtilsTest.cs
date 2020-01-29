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

using Xunit;

namespace Steeltoe.Common.Util.Test
{
    public class PatternMatchUtilsTest
    {
        [Fact]
        public void TestTrivial()
        {
            Assert.False(PatternMatchUtils.SimpleMatch((string)null, string.Empty));
            Assert.False(PatternMatchUtils.SimpleMatch("1", null));
            DoTest("*", "123", true);
            DoTest("123", "123", true);
        }

        [Fact]
        public void TestStartsWith()
        {
            DoTest("get*", "getMe", true);
            DoTest("get*", "setMe", false);
        }

        [Fact]
        public void TestEndsWith()
        {
            DoTest("*Test", "getMeTest", true);
            DoTest("*Test", "setMe", false);
        }

        [Fact]
        public void TestBetween()
        {
            DoTest("*stuff*", "getMeTest", false);
            DoTest("*stuff*", "getstuffTest", true);
            DoTest("*stuff*", "stuffTest", true);
            DoTest("*stuff*", "getstuff", true);
            DoTest("*stuff*", "stuff", true);
        }

        [Fact]
        public void TestStartsEnds()
        {
            DoTest("on*Event", "onMyEvent", true);
            DoTest("on*Event", "onEvent", true);
            DoTest("3*3", "3", false);
            DoTest("3*3", "33", true);
        }

        [Fact]
        public void TestStartsEndsBetween()
        {
            DoTest("12*45*78", "12345678", true);
            DoTest("12*45*78", "123456789", false);
            DoTest("12*45*78", "012345678", false);
            DoTest("12*45*78", "124578", true);
            DoTest("12*45*78", "1245457878", true);
            DoTest("3*3*3", "33", false);
            DoTest("3*3*3", "333", true);
        }

        [Fact]
        public void TestRidiculous()
        {
            DoTest("*1*2*3*", "0011002001010030020201030", true);
            DoTest("1*2*3*4", "10300204", false);
            DoTest("1*2*3*3", "10300203", false);
            DoTest("*1*2*3*", "123", true);
            DoTest("*1*2*3*", "132", false);
        }

        [Fact]
        public void TestPatternVariants()
        {
            DoTest("*a", "*", false);
            DoTest("*a", "a", true);
            DoTest("*a", "b", false);
            DoTest("*a", "aa", true);
            DoTest("*a", "ba", true);
            DoTest("*a", "ab", false);
            DoTest("**a", "*", false);
            DoTest("**a", "a", true);
            DoTest("**a", "b", false);
            DoTest("**a", "aa", true);
            DoTest("**a", "ba", true);
            DoTest("**a", "ab", false);
        }

        private void DoTest(string pattern, string str, bool shouldMatch)
        {
            Assert.Equal(shouldMatch, PatternMatchUtils.SimpleMatch(pattern, str));
        }
    }
}
