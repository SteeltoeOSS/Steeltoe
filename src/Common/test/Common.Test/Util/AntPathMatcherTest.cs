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
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Common.Util.Test
{
    public class AntPathMatcherTest
    {
        private readonly AntPathMatcher pathMatcher = new AntPathMatcher();

        [Fact]
        public void Match()
        {
            // test exact Matching
            Assert.True(pathMatcher.Match("test", "test"));
            Assert.True(pathMatcher.Match("/test", "/test"));

            // SPR-14141
            Assert.True(pathMatcher.Match("https://example.org", "https://example.org"));
            Assert.False(pathMatcher.Match("/test.jpg", "test.jpg"));
            Assert.False(pathMatcher.Match("test", "/test"));
            Assert.False(pathMatcher.Match("/test", "test"));

            // test Matching with ?'s
            Assert.True(pathMatcher.Match("t?st", "test"));
            Assert.True(pathMatcher.Match("??st", "test"));
            Assert.True(pathMatcher.Match("tes?", "test"));
            Assert.True(pathMatcher.Match("te??", "test"));
            Assert.True(pathMatcher.Match("?es?", "test"));
            Assert.False(pathMatcher.Match("tes?", "tes"));
            Assert.False(pathMatcher.Match("tes?", "testt"));
            Assert.False(pathMatcher.Match("tes?", "tsst"));

            // test Matching with *'s
            Assert.True(pathMatcher.Match("*", "test"));
            Assert.True(pathMatcher.Match("test*", "test"));
            Assert.True(pathMatcher.Match("test*", "testTest"));
            Assert.True(pathMatcher.Match("test/*", "test/Test"));
            Assert.True(pathMatcher.Match("test/*", "test/t"));
            Assert.True(pathMatcher.Match("test/*", "test/"));
            Assert.True(pathMatcher.Match("*test*", "AnothertestTest"));
            Assert.True(pathMatcher.Match("*test", "Anothertest"));
            Assert.True(pathMatcher.Match("*.*", "test."));
            Assert.True(pathMatcher.Match("*.*", "test.test"));
            Assert.True(pathMatcher.Match("*.*", "test.test.test"));
            Assert.True(pathMatcher.Match("test*aaa", "testblaaaa"));
            Assert.False(pathMatcher.Match("test*", "tst"));
            Assert.False(pathMatcher.Match("test*", "tsttest"));
            Assert.False(pathMatcher.Match("test*", "test/"));
            Assert.False(pathMatcher.Match("test*", "test/t"));
            Assert.False(pathMatcher.Match("test/*", "test"));
            Assert.False(pathMatcher.Match("*test*", "tsttst"));
            Assert.False(pathMatcher.Match("*test", "tsttst"));
            Assert.False(pathMatcher.Match("*.*", "tsttst"));
            Assert.False(pathMatcher.Match("test*aaa", "test"));
            Assert.False(pathMatcher.Match("test*aaa", "testblaaab"));

            // test Matching with ?'s and /'s
            Assert.True(pathMatcher.Match("/?", "/a"));
            Assert.True(pathMatcher.Match("/?/a", "/a/a"));
            Assert.True(pathMatcher.Match("/a/?", "/a/b"));
            Assert.True(pathMatcher.Match("/??/a", "/aa/a"));
            Assert.True(pathMatcher.Match("/a/??", "/a/bb"));
            Assert.True(pathMatcher.Match("/?", "/a"));

            // test Matching with **'s
            Assert.True(pathMatcher.Match("/**", "/testing/testing"));
            Assert.True(pathMatcher.Match("/*/**", "/testing/testing"));
            Assert.True(pathMatcher.Match("/**/*", "/testing/testing"));
            Assert.True(pathMatcher.Match("/bla/**/bla", "/bla/testing/testing/bla"));
            Assert.True(pathMatcher.Match("/bla/**/bla", "/bla/testing/testing/bla/bla"));
            Assert.True(pathMatcher.Match("/**/test", "/bla/bla/test"));
            Assert.True(pathMatcher.Match("/bla/**/**/bla", "/bla/bla/bla/bla/bla/bla"));
            Assert.True(pathMatcher.Match("/bla*bla/test", "/blaXXXbla/test"));
            Assert.True(pathMatcher.Match("/*bla/test", "/XXXbla/test"));
            Assert.False(pathMatcher.Match("/bla*bla/test", "/blaXXXbl/test"));
            Assert.False(pathMatcher.Match("/*bla/test", "XXXblab/test"));
            Assert.False(pathMatcher.Match("/*bla/test", "XXXbl/test"));

            Assert.False(pathMatcher.Match("/????", "/bala/bla"));
            Assert.False(pathMatcher.Match("/**/*bla", "/bla/bla/bla/bbb"));

            Assert.True(pathMatcher.Match("/*bla*/**/bla/**", "/XXXblaXXXX/testing/testing/bla/testing/testing/"));
            Assert.True(pathMatcher.Match("/*bla*/**/bla/*", "/XXXblaXXXX/testing/testing/bla/testing"));
            Assert.True(pathMatcher.Match("/*bla*/**/bla/**", "/XXXblaXXXX/testing/testing/bla/testing/testing"));
            Assert.True(pathMatcher.Match("/*bla*/**/bla/**", "/XXXblaXXXX/testing/testing/bla/testing/testing.jpg"));

            Assert.True(pathMatcher.Match("*bla*/**/bla/**", "XXXblaXXXX/testing/testing/bla/testing/testing/"));
            Assert.True(pathMatcher.Match("*bla*/**/bla/*", "XXXblaXXXX/testing/testing/bla/testing"));
            Assert.True(pathMatcher.Match("*bla*/**/bla/**", "XXXblaXXXX/testing/testing/bla/testing/testing"));
            Assert.False(pathMatcher.Match("*bla*/**/bla/*", "XXXblaXXXX/testing/testing/bla/testing/testing"));

            Assert.False(pathMatcher.Match("/x/x/**/bla", "/x/x/x/"));

            Assert.True(pathMatcher.Match("/foo/bar/**", "/foo/bar"));

            Assert.True(pathMatcher.Match(string.Empty, string.Empty));

            Assert.True(pathMatcher.Match("/{bla}.*", "/testing.html"));
        }

        [Fact]
        public void MatchWithTrimTokensEnabled()
        {
            pathMatcher.TrimTokens = true;

            Assert.True(pathMatcher.Match("/foo/bar", "/foo /bar"));
        }

        [Fact]
        public void WithMatchStart()
        {
            // test exact Matching
            Assert.True(pathMatcher.MatchStart("test", "test"));
            Assert.True(pathMatcher.MatchStart("/test", "/test"));
            Assert.False(pathMatcher.MatchStart("/test.jpg", "test.jpg"));
            Assert.False(pathMatcher.MatchStart("test", "/test"));
            Assert.False(pathMatcher.MatchStart("/test", "test"));

            // test Matching with ?'s
            Assert.True(pathMatcher.MatchStart("t?st", "test"));
            Assert.True(pathMatcher.MatchStart("??st", "test"));
            Assert.True(pathMatcher.MatchStart("tes?", "test"));
            Assert.True(pathMatcher.MatchStart("te??", "test"));
            Assert.True(pathMatcher.MatchStart("?es?", "test"));
            Assert.False(pathMatcher.MatchStart("tes?", "tes"));
            Assert.False(pathMatcher.MatchStart("tes?", "testt"));
            Assert.False(pathMatcher.MatchStart("tes?", "tsst"));

            // test Matching with *'s
            Assert.True(pathMatcher.MatchStart("*", "test"));
            Assert.True(pathMatcher.MatchStart("test*", "test"));
            Assert.True(pathMatcher.MatchStart("test*", "testTest"));
            Assert.True(pathMatcher.MatchStart("test/*", "test/Test"));
            Assert.True(pathMatcher.MatchStart("test/*", "test/t"));
            Assert.True(pathMatcher.MatchStart("test/*", "test/"));
            Assert.True(pathMatcher.MatchStart("*test*", "AnothertestTest"));
            Assert.True(pathMatcher.MatchStart("*test", "Anothertest"));
            Assert.True(pathMatcher.MatchStart("*.*", "test."));
            Assert.True(pathMatcher.MatchStart("*.*", "test.test"));
            Assert.True(pathMatcher.MatchStart("*.*", "test.test.test"));
            Assert.True(pathMatcher.MatchStart("test*aaa", "testblaaaa"));
            Assert.False(pathMatcher.MatchStart("test*", "tst"));
            Assert.False(pathMatcher.MatchStart("test*", "test/"));
            Assert.False(pathMatcher.MatchStart("test*", "tsttest"));
            Assert.False(pathMatcher.MatchStart("test*", "test/"));
            Assert.False(pathMatcher.MatchStart("test*", "test/t"));
            Assert.True(pathMatcher.MatchStart("test/*", "test"));
            Assert.True(pathMatcher.MatchStart("test/t*.txt", "test"));
            Assert.False(pathMatcher.MatchStart("*test*", "tsttst"));
            Assert.False(pathMatcher.MatchStart("*test", "tsttst"));
            Assert.False(pathMatcher.MatchStart("*.*", "tsttst"));
            Assert.False(pathMatcher.MatchStart("test*aaa", "test"));
            Assert.False(pathMatcher.MatchStart("test*aaa", "testblaaab"));

            // test Matching with ?'s and /'s
            Assert.True(pathMatcher.MatchStart("/?", "/a"));
            Assert.True(pathMatcher.MatchStart("/?/a", "/a/a"));
            Assert.True(pathMatcher.MatchStart("/a/?", "/a/b"));
            Assert.True(pathMatcher.MatchStart("/??/a", "/aa/a"));
            Assert.True(pathMatcher.MatchStart("/a/??", "/a/bb"));
            Assert.True(pathMatcher.MatchStart("/?", "/a"));

            // test Matching with **'s
            Assert.True(pathMatcher.MatchStart("/**", "/testing/testing"));
            Assert.True(pathMatcher.MatchStart("/*/**", "/testing/testing"));
            Assert.True(pathMatcher.MatchStart("/**/*", "/testing/testing"));
            Assert.True(pathMatcher.MatchStart("test*/**", "test/"));
            Assert.True(pathMatcher.MatchStart("test*/**", "test/t"));
            Assert.True(pathMatcher.MatchStart("/bla/**/bla", "/bla/testing/testing/bla"));
            Assert.True(pathMatcher.MatchStart("/bla/**/bla", "/bla/testing/testing/bla/bla"));
            Assert.True(pathMatcher.MatchStart("/**/test", "/bla/bla/test"));
            Assert.True(pathMatcher.MatchStart("/bla/**/**/bla", "/bla/bla/bla/bla/bla/bla"));
            Assert.True(pathMatcher.MatchStart("/bla*bla/test", "/blaXXXbla/test"));
            Assert.True(pathMatcher.MatchStart("/*bla/test", "/XXXbla/test"));
            Assert.False(pathMatcher.MatchStart("/bla*bla/test", "/blaXXXbl/test"));
            Assert.False(pathMatcher.MatchStart("/*bla/test", "XXXblab/test"));
            Assert.False(pathMatcher.MatchStart("/*bla/test", "XXXbl/test"));

            Assert.False(pathMatcher.MatchStart("/????", "/bala/bla"));
            Assert.True(pathMatcher.MatchStart("/**/*bla", "/bla/bla/bla/bbb"));

            Assert.True(pathMatcher.MatchStart("/*bla*/**/bla/**", "/XXXblaXXXX/testing/testing/bla/testing/testing/"));
            Assert.True(pathMatcher.MatchStart("/*bla*/**/bla/*", "/XXXblaXXXX/testing/testing/bla/testing"));
            Assert.True(pathMatcher.MatchStart("/*bla*/**/bla/**", "/XXXblaXXXX/testing/testing/bla/testing/testing"));
            Assert.True(pathMatcher.MatchStart("/*bla*/**/bla/**", "/XXXblaXXXX/testing/testing/bla/testing/testing.jpg"));

            Assert.True(pathMatcher.MatchStart("*bla*/**/bla/**", "XXXblaXXXX/testing/testing/bla/testing/testing/"));
            Assert.True(pathMatcher.MatchStart("*bla*/**/bla/*", "XXXblaXXXX/testing/testing/bla/testing"));
            Assert.True(pathMatcher.MatchStart("*bla*/**/bla/**", "XXXblaXXXX/testing/testing/bla/testing/testing"));
            Assert.True(pathMatcher.MatchStart("*bla*/**/bla/*", "XXXblaXXXX/testing/testing/bla/testing/testing"));

            Assert.True(pathMatcher.MatchStart("/x/x/**/bla", "/x/x/x/"));

            Assert.True(pathMatcher.MatchStart(string.Empty, string.Empty));
        }

        [Fact]
        public void UniqueDeliminator()
        {
            pathMatcher.PathSeparator = ".";

            // test exact Matching
            Assert.True(pathMatcher.Match("test", "test"));
            Assert.True(pathMatcher.Match(".test", ".test"));
            Assert.False(pathMatcher.Match(".test/jpg", "test/jpg"));
            Assert.False(pathMatcher.Match("test", ".test"));
            Assert.False(pathMatcher.Match(".test", "test"));

            // test Matching with ?'s
            Assert.True(pathMatcher.Match("t?st", "test"));
            Assert.True(pathMatcher.Match("??st", "test"));
            Assert.True(pathMatcher.Match("tes?", "test"));
            Assert.True(pathMatcher.Match("te??", "test"));
            Assert.True(pathMatcher.Match("?es?", "test"));
            Assert.False(pathMatcher.Match("tes?", "tes"));
            Assert.False(pathMatcher.Match("tes?", "testt"));
            Assert.False(pathMatcher.Match("tes?", "tsst"));

            // test Matching with *'s
            Assert.True(pathMatcher.Match("*", "test"));
            Assert.True(pathMatcher.Match("test*", "test"));
            Assert.True(pathMatcher.Match("test*", "testTest"));
            Assert.True(pathMatcher.Match("*test*", "AnothertestTest"));
            Assert.True(pathMatcher.Match("*test", "Anothertest"));
            Assert.True(pathMatcher.Match("*/*", "test/"));
            Assert.True(pathMatcher.Match("*/*", "test/test"));
            Assert.True(pathMatcher.Match("*/*", "test/test/test"));
            Assert.True(pathMatcher.Match("test*aaa", "testblaaaa"));
            Assert.False(pathMatcher.Match("test*", "tst"));
            Assert.False(pathMatcher.Match("test*", "tsttest"));
            Assert.False(pathMatcher.Match("*test*", "tsttst"));
            Assert.False(pathMatcher.Match("*test", "tsttst"));
            Assert.False(pathMatcher.Match("*/*", "tsttst"));
            Assert.False(pathMatcher.Match("test*aaa", "test"));
            Assert.False(pathMatcher.Match("test*aaa", "testblaaab"));

            // test Matching with ?'s and .'s
            Assert.True(pathMatcher.Match(".?", ".a"));
            Assert.True(pathMatcher.Match(".?.a", ".a.a"));
            Assert.True(pathMatcher.Match(".a.?", ".a.b"));
            Assert.True(pathMatcher.Match(".??.a", ".aa.a"));
            Assert.True(pathMatcher.Match(".a.??", ".a.bb"));
            Assert.True(pathMatcher.Match(".?", ".a"));

            // test Matching with **'s
            Assert.True(pathMatcher.Match(".**", ".testing.testing"));
            Assert.True(pathMatcher.Match(".*.**", ".testing.testing"));
            Assert.True(pathMatcher.Match(".**.*", ".testing.testing"));
            Assert.True(pathMatcher.Match(".bla.**.bla", ".bla.testing.testing.bla"));
            Assert.True(pathMatcher.Match(".bla.**.bla", ".bla.testing.testing.bla.bla"));
            Assert.True(pathMatcher.Match(".**.test", ".bla.bla.test"));
            Assert.True(pathMatcher.Match(".bla.**.**.bla", ".bla.bla.bla.bla.bla.bla"));
            Assert.True(pathMatcher.Match(".bla*bla.test", ".blaXXXbla.test"));
            Assert.True(pathMatcher.Match(".*bla.test", ".XXXbla.test"));
            Assert.False(pathMatcher.Match(".bla*bla.test", ".blaXXXbl.test"));
            Assert.False(pathMatcher.Match(".*bla.test", "XXXblab.test"));
            Assert.False(pathMatcher.Match(".*bla.test", "XXXbl.test"));
        }

        [Fact]
        public void ExtractPathWithinPattern()
        {
            Assert.Equal(string.Empty, pathMatcher.ExtractPathWithinPattern("/docs/commit.html", "/docs/commit.html"));

            Assert.Equal("cvs/commit", pathMatcher.ExtractPathWithinPattern("/docs/*", "/docs/cvs/commit"));
            Assert.Equal("commit.html", pathMatcher.ExtractPathWithinPattern("/docs/cvs/*.html", "/docs/cvs/commit.html"));
            Assert.Equal("cvs/commit", pathMatcher.ExtractPathWithinPattern("/docs/**", "/docs/cvs/commit"));
            Assert.Equal("cvs/commit.html", pathMatcher.ExtractPathWithinPattern("/docs/**/*.html", "/docs/cvs/commit.html"));
            Assert.Equal("commit.html", pathMatcher.ExtractPathWithinPattern("/docs/**/*.html", "/docs/commit.html"));
            Assert.Equal("commit.html", pathMatcher.ExtractPathWithinPattern("/*.html", "/commit.html"));
            Assert.Equal("docs/commit.html", pathMatcher.ExtractPathWithinPattern("/*.html", "/docs/commit.html"));
            Assert.Equal("/commit.html", pathMatcher.ExtractPathWithinPattern("*.html", "/commit.html"));
            Assert.Equal("/docs/commit.html", pathMatcher.ExtractPathWithinPattern("*.html", "/docs/commit.html"));
            Assert.Equal("/docs/commit.html", pathMatcher.ExtractPathWithinPattern("**/*.*", "/docs/commit.html"));
            Assert.Equal("/docs/commit.html", pathMatcher.ExtractPathWithinPattern("*", "/docs/commit.html"));

            // SPR-10515
            Assert.Equal("/docs/cvs/other/commit.html", pathMatcher.ExtractPathWithinPattern("**/commit.html", "/docs/cvs/other/commit.html"));
            Assert.Equal("cvs/other/commit.html", pathMatcher.ExtractPathWithinPattern("/docs/**/commit.html", "/docs/cvs/other/commit.html"));
            Assert.Equal("cvs/other/commit.html", pathMatcher.ExtractPathWithinPattern("/docs/**/**/**/**", "/docs/cvs/other/commit.html"));

            Assert.Equal("docs/cvs/commit", pathMatcher.ExtractPathWithinPattern("/d?cs/*", "/docs/cvs/commit"));
            Assert.Equal("cvs/commit.html", pathMatcher.ExtractPathWithinPattern("/docs/c?s/*.html", "/docs/cvs/commit.html"));
            Assert.Equal("docs/cvs/commit", pathMatcher.ExtractPathWithinPattern("/d?cs/**", "/docs/cvs/commit"));
            Assert.Equal("docs/cvs/commit.html", pathMatcher.ExtractPathWithinPattern("/d?cs/**/*.html", "/docs/cvs/commit.html"));
        }

        [Fact]
        public void ExtractUriTemplateVariables()
        {
            var result = pathMatcher.ExtractUriTemplateVariables("/hotels/{hotel}", "/hotels/1");
            Assert.Equal(new Dictionary<string, string>() { { "hotel", "1" } }, result);

            result = pathMatcher.ExtractUriTemplateVariables("/h?tels/{hotel}", "/hotels/1");
            Assert.Equal(new Dictionary<string, string>() { { "hotel", "1" } }, result);

            result = pathMatcher.ExtractUriTemplateVariables("/hotels/{hotel}/bookings/{booking}", "/hotels/1/bookings/2");
            IDictionary<string, string> expected = new Dictionary<string, string>
            {
                { "hotel", "1" },
                { "booking", "2" }
            };
            Assert.Equal(expected, result);

            result = pathMatcher.ExtractUriTemplateVariables("/**/hotels/**/{hotel}", "/foo/hotels/bar/1");
            Assert.Equal(new Dictionary<string, string>() { { "hotel", "1" } }, result);

            result = pathMatcher.ExtractUriTemplateVariables("/{page}.html", "/42.html");
            Assert.Equal(new Dictionary<string, string>() { { "page", "42" } }, result);

            result = pathMatcher.ExtractUriTemplateVariables("/{page}.*", "/42.html");
            Assert.Equal(new Dictionary<string, string>() { { "page", "42" } }, result);

            result = pathMatcher.ExtractUriTemplateVariables("/A-{B}-C", "/A-b-C");
            Assert.Equal(new Dictionary<string, string>() { { "B", "b" } }, result);

            result = pathMatcher.ExtractUriTemplateVariables("/{name}.{extension}", "/test.html");
            expected.Clear();
            expected.Add("name", "test");
            expected.Add("extension", "html");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ExtractUriTemplateVariablesRegex()
        {
            var result = pathMatcher
                    .ExtractUriTemplateVariables(
                            "{symbolicName:[\\w\\.]+}-{version:[\\w\\.]+}.jar",
                            "com.example-1.0.0.jar");
            Assert.Equal("com.example", result["symbolicName"]);
            Assert.Equal("1.0.0", result["version"]);

            result = pathMatcher.ExtractUriTemplateVariables(
                            "{symbolicName:[\\w\\.]+}-sources-{version:[\\w\\.]+}.jar",
                            "com.example-sources-1.0.0.jar");
            Assert.Equal("com.example", result["symbolicName"]);
            Assert.Equal("1.0.0", result["version"]);
        }

        [Fact]
        public void ExtractUriTemplateVarsRegexQualifiers()
        {
            var result = pathMatcher.ExtractUriTemplateVariables(
                    "{symbolicName:[\\p{L}\\.]+}-sources-{version:[\\p{N}\\.]+}.jar",
                    "com.example-sources-1.0.0.jar");
            Assert.Equal("com.example", result["symbolicName"]);
            Assert.Equal("1.0.0", result["version"]);

            result = pathMatcher.ExtractUriTemplateVariables(
                    "{symbolicName:[\\w\\.]+}-sources-{version:[\\d\\.]+}-{year:\\d{4}}{month:\\d{2}}{day:\\d{2}}.jar",
                    "com.example-sources-1.0.0-20100220.jar");
            Assert.Equal("com.example", result["symbolicName"]);
            Assert.Equal("1.0.0", result["version"]);
            Assert.Equal("2010", result["year"]);
            Assert.Equal("02", result["month"]);
            Assert.Equal("20", result["day"]);

            result = pathMatcher.ExtractUriTemplateVariables(
                    "{symbolicName:[\\p{L}\\.]+}-sources-{version:[\\p{N}\\.\\{\\}]+}.jar",
                    "com.example-sources-1.0.0.{12}.jar");
            Assert.Equal("com.example", result["symbolicName"]);
            Assert.Equal("1.0.0.{12}", result["version"]);
        }

        [Fact]
        public void ExtractUriTemplateVarsRegexCapturingGroups()
        {
            Assert.Throws<InvalidOperationException>(() => pathMatcher.ExtractUriTemplateVariables("/web/{id:foo(bar)?}", "/web/foobar"));
        }

        [Fact]
        public void Combine()
        {
            Assert.Equal(string.Empty, pathMatcher.Combine(null, null));
            Assert.Equal("/hotels", pathMatcher.Combine("/hotels", null));
            Assert.Equal("/hotels", pathMatcher.Combine(null, "/hotels"));
            Assert.Equal("/hotels/booking", pathMatcher.Combine("/hotels/*", "booking"));
            Assert.Equal("/hotels/booking", pathMatcher.Combine("/hotels/*", "/booking"));
            Assert.Equal("/hotels/**/booking", pathMatcher.Combine("/hotels/**", "booking"));
            Assert.Equal("/hotels/**/booking", pathMatcher.Combine("/hotels/**", "/booking"));
            Assert.Equal("/hotels/booking", pathMatcher.Combine("/hotels", "/booking"));
            Assert.Equal("/hotels/booking", pathMatcher.Combine("/hotels", "booking"));
            Assert.Equal("/hotels/booking", pathMatcher.Combine("/hotels/", "booking"));
            Assert.Equal("/hotels/{hotel}", pathMatcher.Combine("/hotels/*", "{hotel}"));
            Assert.Equal("/hotels/**/{hotel}", pathMatcher.Combine("/hotels/**", "{hotel}"));
            Assert.Equal("/hotels/{hotel}", pathMatcher.Combine("/hotels", "{hotel}"));
            Assert.Equal("/hotels/{hotel}.*", pathMatcher.Combine("/hotels", "{hotel}.*"));
            Assert.Equal("/hotels/*/booking/{booking}", pathMatcher.Combine("/hotels/*/booking", "{booking}"));
            Assert.Equal("/hotel.html", pathMatcher.Combine("/*.html", "/hotel.html"));
            Assert.Equal("/hotel.html", pathMatcher.Combine("/*.html", "/hotel"));
            Assert.Equal("/hotel.html", pathMatcher.Combine("/*.html", "/hotel.*"));
            Assert.Equal("/*.html", pathMatcher.Combine("/**", "/*.html"));
            Assert.Equal("/*.html", pathMatcher.Combine("/*", "/*.html"));
            Assert.Equal("/*.html", pathMatcher.Combine("/*.*", "/*.html"));

            // SPR-8858
            Assert.Equal("/{foo}/bar", pathMatcher.Combine("/{foo}", "/bar"));

            // SPR-7970
            Assert.Equal("/user/user", pathMatcher.Combine("/user", "/user"));

            // SPR-10062
            Assert.Equal("/{foo:.*[^0-9].*}/edit/", pathMatcher.Combine("/{foo:.*[^0-9].*}", "/edit/"));

            // SPR-10554
            Assert.Equal("/1.0/foo/test", pathMatcher.Combine("/1.0", "/foo/test"));

            // SPR-12975
            Assert.Equal("/hotel", pathMatcher.Combine("/", "/hotel"));

            // SPR-12975
            Assert.Equal("/hotel/booking", pathMatcher.Combine("/hotel/", "/booking"));
        }

        [Fact]
        public void PatternComparator()
        {
            var comparator = pathMatcher.GetPatternComparer("/hotels/new");

            Assert.Equal(0, comparator.Compare(null, null));
            Assert.Equal(1, comparator.Compare(null, "/hotels/new"));
            Assert.Equal(-1, comparator.Compare("/hotels/new", null));

            Assert.Equal(0, comparator.Compare("/hotels/new", "/hotels/new"));

            Assert.Equal(-1, comparator.Compare("/hotels/new", "/hotels/*"));
            Assert.Equal(1, comparator.Compare("/hotels/*", "/hotels/new"));
            Assert.Equal(0, comparator.Compare("/hotels/*", "/hotels/*"));

            Assert.Equal(-1, comparator.Compare("/hotels/new", "/hotels/{hotel}"));
            Assert.Equal(1, comparator.Compare("/hotels/{hotel}", "/hotels/new"));
            Assert.Equal(0, comparator.Compare("/hotels/{hotel}", "/hotels/{hotel}"));
            Assert.Equal(-1, comparator.Compare("/hotels/{hotel}/booking", "/hotels/{hotel}/bookings/{booking}"));
            Assert.Equal(1, comparator.Compare("/hotels/{hotel}/bookings/{booking}", "/hotels/{hotel}/booking"));

            // SPR-10550
            Assert.Equal(-1, comparator.Compare("/hotels/{hotel}/bookings/{booking}/cutomers/{customer}", "/**"));
            Assert.Equal(1, comparator.Compare("/**", "/hotels/{hotel}/bookings/{booking}/cutomers/{customer}"));
            Assert.Equal(0, comparator.Compare("/**", "/**"));

            Assert.Equal(-1, comparator.Compare("/hotels/{hotel}", "/hotels/*"));
            Assert.Equal(1, comparator.Compare("/hotels/*", "/hotels/{hotel}"));

            Assert.Equal(-1, comparator.Compare("/hotels/*", "/hotels/*/**"));
            Assert.Equal(1, comparator.Compare("/hotels/*/**", "/hotels/*"));

            Assert.Equal(-1, comparator.Compare("/hotels/new", "/hotels/new.*"));
            Assert.Equal(2, comparator.Compare("/hotels/{hotel}", "/hotels/{hotel}.*"));

            // SPR-6741
            Assert.Equal(-1, comparator.Compare("/hotels/{hotel}/bookings/{booking}/cutomers/{customer}", "/hotels/**"));
            Assert.Equal(1, comparator.Compare("/hotels/**", "/hotels/{hotel}/bookings/{booking}/cutomers/{customer}"));
            Assert.Equal(1, comparator.Compare("/hotels/foo/bar/**", "/hotels/{hotel}"));
            Assert.Equal(-1, comparator.Compare("/hotels/{hotel}", "/hotels/foo/bar/**"));
            Assert.Equal(2, comparator.Compare("/hotels/**/bookings/**", "/hotels/**"));
            Assert.Equal(-2, comparator.Compare("/hotels/**", "/hotels/**/bookings/**"));

            // SPR-8683
            Assert.Equal(1, comparator.Compare("/**", "/hotels/{hotel}"));

            // longer is better
            Assert.Equal(1, comparator.Compare("/hotels", "/hotels2"));

            // SPR-13139
            Assert.Equal(-1, comparator.Compare("*", "*/**"));
            Assert.Equal(1, comparator.Compare("*/**", "*"));
        }

        [Fact]
        public void PatternComparatorSort()
        {
            var comparator = pathMatcher.GetPatternComparer("/hotels/new");

            var paths = new List<string>(3)
            {
                null,
                "/hotels/new"
            };
            paths.Sort(comparator);
            Assert.Equal("/hotels/new", paths[0]);
            Assert.Null(paths[1]);
            paths.Clear();

            paths.Add("/hotels/new");
            paths.Add(null);
            paths.Sort(comparator);
            Assert.Equal("/hotels/new", paths[0]);
            Assert.Null(paths[1]);
            paths.Clear();

            paths.Add("/hotels/*");
            paths.Add("/hotels/new");
            paths.Sort(comparator);
            Assert.Equal("/hotels/new", paths[0]);
            Assert.Equal("/hotels/*", paths[1]);
            paths.Clear();

            paths.Add("/hotels/new");
            paths.Add("/hotels/*");
            paths.Sort(comparator);
            Assert.Equal("/hotels/new", paths[0]);
            Assert.Equal("/hotels/*", paths[1]);
            paths.Clear();

            paths.Add("/hotels/**");
            paths.Add("/hotels/*");
            paths.Sort(comparator);
            Assert.Equal("/hotels/*", paths[0]);
            Assert.Equal("/hotels/**", paths[1]);
            paths.Clear();

            paths.Add("/hotels/*");
            paths.Add("/hotels/**");
            paths.Sort(comparator);
            Assert.Equal("/hotels/*", paths[0]);
            Assert.Equal("/hotels/**", paths[1]);
            paths.Clear();

            paths.Add("/hotels/{hotel}");
            paths.Add("/hotels/new");
            paths.Sort(comparator);
            Assert.Equal("/hotels/new", paths[0]);
            Assert.Equal("/hotels/{hotel}", paths[1]);
            paths.Clear();

            paths.Add("/hotels/new");
            paths.Add("/hotels/{hotel}");
            paths.Sort(comparator);
            Assert.Equal("/hotels/new", paths[0]);
            Assert.Equal("/hotels/{hotel}", paths[1]);
            paths.Clear();

            paths.Add("/hotels/*");
            paths.Add("/hotels/{hotel}");
            paths.Add("/hotels/new");
            paths.Sort(comparator);
            Assert.Equal("/hotels/new", paths[0]);
            Assert.Equal("/hotels/{hotel}", paths[1]);
            Assert.Equal("/hotels/*", paths[2]);
            paths.Clear();

            paths.Add("/hotels/ne*");
            paths.Add("/hotels/n*");
            Shuffle(paths);
            paths.Sort(comparator);
            Assert.Equal("/hotels/ne*", paths[0]);
            Assert.Equal("/hotels/n*", paths[1]);
            paths.Clear();

            comparator = pathMatcher.GetPatternComparer("/hotels/new.html");
            paths.Add("/hotels/new.*");
            paths.Add("/hotels/{hotel}");
            Shuffle(paths);
            paths.Sort(comparator);
            Assert.Equal("/hotels/new.*", paths[0]);
            Assert.Equal("/hotels/{hotel}", paths[1]);
            paths.Clear();

            comparator = pathMatcher.GetPatternComparer("/web/endUser/action/login.html");
            paths.Add("/**/login.*");
            paths.Add("/**/endUser/action/login.*");
            paths.Sort(comparator);
            Assert.Equal("/**/endUser/action/login.*", paths[0]);
            Assert.Equal("/**/login.*", paths[1]);
            paths.Clear();
        }

        private static Random rng = new Random();

        private static void Shuffle<T>(List<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
