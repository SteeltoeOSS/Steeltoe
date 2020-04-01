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

using Steeltoe.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.Services.Test
{
    public class ServiceInfoFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfSchemeNull()
        {
            string scheme = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new TestServiceInfoFactory(new Tags("foo"), scheme));
            Assert.Contains(nameof(scheme), ex.Message);
        }

        [Fact]
        public void Constructor_ThrowsIfTagsNull()
        {
            var scheme = "scheme";
            Tags tags = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new TestServiceInfoFactory(tags, scheme));
            Assert.Contains(nameof(tags), ex.Message);
        }

        [Fact]
        public void TagsMatch_Matches()
        {
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var service1 = new Service()
            {
                Tags = new string[] { "bar" }
            };
            Assert.True(sif.TagsMatch(service1));
        }

        [Fact]
        public void TagsMatch_DoesntMatch()
        {
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var service1 = new Service()
            {
                Tags = new string[] { "noMatch" }
            };
            Assert.False(sif.TagsMatch(service1));
        }

        [Fact]
        public void LabelStartsWithTag_Matches()
        {
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var service1 = new Service()
            {
                Tags = new string[] { "noMatch" },
                Label = "foobarfoo"
            };
            Assert.True(sif.LabelStartsWithTag(service1));
        }

        [Fact]
        public void LabelStartsWithTag_DoesntMatch()
        {
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var service1 = new Service()
            {
                Tags = new string[] { "noMatch" },
                Label = "baby"
            };
            Assert.False(sif.LabelStartsWithTag(service1));
        }

        [Fact]
        public void UriMatchesScheme_Matches()
        {
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var service1 = new Service()
            {
                Tags = new string[] { "noMatch" },
                Label = "noMatch",
                Credentials = new Dictionary<string, Credential>()
                {
                    { "uri", new Credential("scheme://foo") }
                }
            };
            Assert.True(sif.UriMatchesScheme(service1));
        }

        [Fact]
        public void UriMatchesScheme_DoesntMatch()
        {
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var service1 = new Service()
            {
                Tags = new string[] { "noMatch" },
                Label = "noMatch",
                Credentials = new Dictionary<string, Credential>()
                {
                    { "uri", new Credential("nomatch://foo") }
                }
            };
            Assert.False(sif.UriMatchesScheme(service1));
        }

        [Fact]
        public void GetUsernameFromCredentials_ReturnsCorrect()
        {
            var credentials = new Dictionary<string, Credential>()
            {
                { "username", new Credential("username") }
            };
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var uname = sif.GetUsernameFromCredentials(credentials);
            Assert.Equal("username", uname);

            credentials = new Dictionary<string, Credential>()
            {
                { "user", new Credential("username") }
            };
            uname = sif.GetUsernameFromCredentials(credentials);
            Assert.Equal("username", uname);

            credentials = new Dictionary<string, Credential>()
            {
            };
            uname = sif.GetUsernameFromCredentials(credentials);
            Assert.Null(uname);
        }

        [Fact]
        public void GetPasswordFromCredentials_ReturnsCorrect()
        {
            var credentials = new Dictionary<string, Credential>()
            {
                { "password", new Credential("password") }
            };
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var pwd = sif.GetPasswordFromCredentials(credentials);
            Assert.Equal("password", pwd);
            credentials = new Dictionary<string, Credential>()
            {
            };
            pwd = sif.GetPasswordFromCredentials(credentials);
            Assert.Null(pwd);
        }

        [Fact]
        public void GetPortFromCredentials_ReturnsCorrect()
        {
            var credentials = new Dictionary<string, Credential>()
            {
                { "port", new Credential("123") }
            };
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var port = sif.GetPortFromCredentials(credentials);
            Assert.Equal(123, port);
            credentials = new Dictionary<string, Credential>()
            {
            };
            port = sif.GetPortFromCredentials(credentials);
            Assert.Equal(0, port);
        }

        [Fact]
        public void GetHostFromCredentials_ReturnsCorrect()
        {
            var credentials = new Dictionary<string, Credential>()
            {
                { "host", new Credential("host") }
            };
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var host = sif.GetHostFromCredentials(credentials);
            Assert.Equal("host", host);

            credentials = new Dictionary<string, Credential>()
            {
                { "hostname", new Credential("hostname") }
            };
            host = sif.GetHostFromCredentials(credentials);
            Assert.Equal("hostname", host);

            credentials = new Dictionary<string, Credential>()
            {
            };
            host = sif.GetHostFromCredentials(credentials);
            Assert.Null(host);
        }

        [Fact]
        public void GetUriFromCredentials_ReturnsCorrect()
        {
            var credentials = new Dictionary<string, Credential>()
            {
                { "uri", new Credential("https://boo:222") }
            };
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var uri = sif.GetUriFromCredentials(credentials);
            Assert.Equal("https://boo:222", uri);

            credentials = new Dictionary<string, Credential>()
            {
                { "url", new Credential("https://boo:222") }
            };
            uri = sif.GetUriFromCredentials(credentials);
            Assert.Equal("https://boo:222", uri);

            credentials = new Dictionary<string, Credential>()
            {
            };
            uri = sif.GetUriFromCredentials(credentials);
            Assert.Null(uri);
        }

        [Fact]
        public void GetListFromCredentials_ReturnsCorrect()
        {
            var credentials = new Dictionary<string, Credential>()
            {
                {
                    "uris", new Credential()
                        {
                            { "0", new Credential("https://foo:11") },
                            { "1", new Credential("https://bar:11") }
                        }
                }
            };
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var list = sif.GetListFromCredentials(credentials, "uris");
            Assert.NotNull(list);
            Assert.Equal(2, list.Count);
            Assert.True(list[0].Equals("https://foo:11") || list[0].Equals("https://bar:11"));
            Assert.True(list[1].Equals("https://foo:11") || list[1].Equals("https://bar:11"));
        }

        [Fact]
        public void GetListFromCredentials_ThrowsWhenListNotPossible()
        {
            var credentials = new Dictionary<string, Credential>()
            {
                {
                    "foo", new Credential()
                        {
                            {
                                "bar", new Credential()
                                {
                                    { "bang", new Credential("badabing") }
                                }
                            },
                        }
                }
            };
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var ex = Assert.Throws<ConnectorException>(() => sif.GetListFromCredentials(credentials, "foo"));
            Assert.Contains("key=foo", ex.Message);
            Assert.Contains("value=bar/", ex.Message);
        }

        [Fact]
        public void GetIntFromCredentials_ThrowsFormatException()
        {
            var credentials = new Dictionary<string, Credential>()
            {
                { "key", new Credential("foobar") }
            };
            var tags = new Tags(new string[] { "foo", "bar" });
            var scheme = "scheme";

            var sif = new TestServiceInfoFactory(tags, scheme);
            var ex = Assert.Throws<FormatException>(() => sif.GetIntFromCredentials(credentials, "key"));
        }
    }
}
