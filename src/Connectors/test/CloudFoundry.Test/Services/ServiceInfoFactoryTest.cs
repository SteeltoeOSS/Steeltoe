// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Configuration;
using Steeltoe.Connectors.CloudFoundry.Services;
using Xunit;

namespace Steeltoe.Connectors.CloudFoundry.Test.Services;

public sealed class ServiceInfoFactoryTest
{
    [Fact]
    public void Constructor_ThrowsIfSchemeNull()
    {
        const string scheme = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new TestServiceInfoFactory(new Tags("foo"), scheme));
        Assert.Contains(nameof(scheme), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_ThrowsIfTagsNull()
    {
        const string scheme = "scheme";
        const Tags tags = null;

        var ex = Assert.Throws<ArgumentNullException>(() => new TestServiceInfoFactory(tags, scheme));
        Assert.Contains(nameof(tags), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TagsMatch_Matches()
    {
        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);

        var service1 = new Service
        {
            Tags =
            {
                "bar"
            }
        };

        Assert.True(sif.TagsMatch(service1));
    }

    [Fact]
    public void TagsMatch_DoesNotMatch()
    {
        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);

        var service1 = new Service
        {
            Tags =
            {
                "noMatch"
            }
        };

        Assert.False(sif.TagsMatch(service1));
    }

    [Fact]
    public void LabelStartsWithTag_Matches()
    {
        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);

        var service1 = new Service
        {
            Tags =
            {
                "noMatch"
            },
            Label = "foobarfoo"
        };

        Assert.True(sif.LabelStartsWithTag(service1));
    }

    [Fact]
    public void LabelStartsWithTag_DoesNotMatch()
    {
        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);

        var service1 = new Service
        {
            Tags =
            {
                "noMatch"
            },
            Label = "baby"
        };

        Assert.False(sif.LabelStartsWithTag(service1));
    }

    [Fact]
    public void UriMatchesScheme_Matches()
    {
        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);

        var service1 = new Service
        {
            Tags =
            {
                "noMatch"
            },
            Label = "noMatch",
            Credentials =
            {
                { "uri", new Credential("scheme://foo") }
            }
        };

        Assert.True(sif.UriMatchesScheme(service1));
    }

    [Fact]
    public void UriMatchesScheme_DoesNotMatch()
    {
        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);

        var service1 = new Service
        {
            Tags =
            {
                "noMatch"
            },
            Label = "noMatch",
            Credentials =
            {
                { "uri", new Credential("nomatch://foo") }
            }
        };

        Assert.False(sif.UriMatchesScheme(service1));
    }

    [Fact]
    public void GetUsernameFromCredentials_ReturnsCorrect()
    {
        var credentials = new Dictionary<string, Credential>
        {
            { "username", new Credential("username") }
        };

        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);
        string username = sif.GetUsernameFromCredentials(credentials);
        Assert.Equal("username", username);

        credentials = new Dictionary<string, Credential>
        {
            { "user", new Credential("username") }
        };

        username = sif.GetUsernameFromCredentials(credentials);
        Assert.Equal("username", username);

        credentials = new Dictionary<string, Credential>();
        username = sif.GetUsernameFromCredentials(credentials);
        Assert.Null(username);
    }

    [Fact]
    public void GetPasswordFromCredentials_ReturnsCorrect()
    {
        var credentials = new Dictionary<string, Credential>
        {
            { "password", new Credential("password") }
        };

        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);
        string pwd = sif.GetPasswordFromCredentials(credentials);
        Assert.Equal("password", pwd);
        credentials = new Dictionary<string, Credential>();
        pwd = sif.GetPasswordFromCredentials(credentials);
        Assert.Null(pwd);
    }

    [Fact]
    public void GetPortFromCredentials_ReturnsCorrect()
    {
        var credentials = new Dictionary<string, Credential>
        {
            { "port", new Credential("123") }
        };

        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);
        int port = sif.GetPortFromCredentials(credentials);
        Assert.Equal(123, port);
        credentials = new Dictionary<string, Credential>();
        port = sif.GetPortFromCredentials(credentials);
        Assert.Equal(0, port);
    }

    [Fact]
    public void GetHostFromCredentials_ReturnsCorrect()
    {
        var credentials = new Dictionary<string, Credential>
        {
            { "host", new Credential("host") }
        };

        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);
        string host = sif.GetHostFromCredentials(credentials);
        Assert.Equal("host", host);

        credentials = new Dictionary<string, Credential>
        {
            { "hostname", new Credential("hostname") }
        };

        host = sif.GetHostFromCredentials(credentials);
        Assert.Equal("hostname", host);

        credentials = new Dictionary<string, Credential>();
        host = sif.GetHostFromCredentials(credentials);
        Assert.Null(host);
    }

    [Fact]
    public void GetUriFromCredentials_ReturnsCorrect()
    {
        var credentials = new Dictionary<string, Credential>
        {
            { "uri", new Credential("https://boo:222") }
        };

        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);
        string uri = sif.GetUriFromCredentials(credentials);
        Assert.Equal("https://boo:222", uri);

        credentials = new Dictionary<string, Credential>
        {
            { "url", new Credential("https://boo:222") }
        };

        uri = sif.GetUriFromCredentials(credentials);
        Assert.Equal("https://boo:222", uri);

        credentials = new Dictionary<string, Credential>();
        uri = sif.GetUriFromCredentials(credentials);
        Assert.Null(uri);
    }

    [Fact]
    public void GetListFromCredentials_ReturnsCorrect()
    {
        var credentials = new Dictionary<string, Credential>
        {
            {
                "uris", new Credential
                {
                    { "0", new Credential("https://foo:11") },
                    { "1", new Credential("https://bar:11") }
                }
            }
        };

        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);
        List<string> list = sif.GetListFromCredentials(credentials, "uris");
        Assert.NotNull(list);
        Assert.Equal(2, list.Count);
        Assert.True(list[0] == "https://foo:11" || list[0] == "https://bar:11");
        Assert.True(list[1] == "https://foo:11" || list[1] == "https://bar:11");
    }

    [Fact]
    public void GetListFromCredentials_ThrowsWhenListNotPossible()
    {
        var credentials = new Dictionary<string, Credential>
        {
            {
                "foo", new Credential
                {
                    {
                        "bar", new Credential
                        {
                            { "bang", new Credential("badabing") }
                        }
                    }
                }
            }
        };

        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);
        var ex = Assert.Throws<ConnectorException>(() => sif.GetListFromCredentials(credentials, "foo"));
        Assert.Contains("key=foo", ex.Message, StringComparison.Ordinal);
        Assert.Contains("value=bar/", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetIntFromCredentials_ThrowsFormatException()
    {
        var credentials = new Dictionary<string, Credential>
        {
            { "key", new Credential("foobar") }
        };

        var tags = new Tags(new[]
        {
            "foo",
            "bar"
        });

        const string scheme = "scheme";

        var sif = new TestServiceInfoFactory(tags, scheme);
        Assert.Throws<FormatException>(() => sif.GetIntFromCredentials(credentials, "key"));
    }
}
