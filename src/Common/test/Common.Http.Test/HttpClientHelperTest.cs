// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Http.Test;

public sealed class HttpClientHelperTest
{
    [Fact]
    public void GetEncodedUserPassword_Nulls()
    {
        string result = HttpClientHelper.GetEncodedUserPassword(null, null);
        Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes($"{string.Empty}:{string.Empty}")), result);

        string result2 = HttpClientHelper.GetEncodedUserPassword("foo", null);
        Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes($"foo:{string.Empty}")), result2);

        string result3 = HttpClientHelper.GetEncodedUserPassword(null, "bar");
        Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes($"{string.Empty}:bar")), result3);
    }

    [Fact]
    public void GetEncodedUserPassword_NotNulls()
    {
        string result = HttpClientHelper.GetEncodedUserPassword("foo", "bar");
        Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes("foo" + ":" + "bar")), result);
    }

    [Fact]
    public void GetRequestMessage_CreatesCorrectMessage()
    {
        HttpRequestMessage message = HttpClientHelper.GetRequestMessage(HttpMethod.Put, new Uri("https://localhost/foobar"), null, null);
        Assert.NotNull(message);
        Assert.Equal(HttpMethod.Put, message.Method);
        Assert.Equal("https://localhost/foobar", message.RequestUri?.ToString());
        Assert.Null(message.Headers.Authorization);
    }

    [Fact]
    public void GetRequestMessage_CreatesCorrectMessage_WithBasicAuth()
    {
        HttpRequestMessage message = HttpClientHelper.GetRequestMessage(HttpMethod.Put, new Uri("https://localhost/foobar"), "foo", "bar");
        Assert.NotNull(message);
        Assert.Equal(HttpMethod.Put, message.Method);
        Assert.Equal("https://localhost/foobar", message.RequestUri?.ToString());
        Assert.NotNull(message.Headers.Authorization);
        string bytes = Convert.ToBase64String(Encoding.ASCII.GetBytes("foo" + ":" + "bar"));
        Assert.Equal("Basic", message.Headers.Authorization.Scheme);
        Assert.Equal(bytes, message.Headers.Authorization.Parameter);
    }
}
