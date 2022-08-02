// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Http.Test;

public class HttpClientHelperTest
{
    [Fact]
    public void GetHttpClient_SetsTimeout()
    {
        HttpClient client = HttpClientHelper.GetHttpClient(false, 100);
        Assert.Equal(100, client.Timeout.TotalMilliseconds);
    }

    [Fact]
    public void ConfigureCertificateValidation_ValidateFalse()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        ServicePointManager.ServerCertificateValidationCallback = null;

        HttpClientHelper.ConfigureCertificateValidation(false, out _, out _);

        if (Platform.IsNetCore)
        {
            Assert.Equal(SecurityProtocolType.Tls, ServicePointManager.SecurityProtocol);
            Assert.Null(ServicePointManager.ServerCertificateValidationCallback);
        }

        if (Platform.IsFullFramework)
        {
            Assert.Equal(SecurityProtocolType.Tls12, ServicePointManager.SecurityProtocol);
            Assert.NotNull(ServicePointManager.ServerCertificateValidationCallback);
        }
    }

    [Fact]
    public void ConfigureCertificateValidation_ValidateTrue()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        ServicePointManager.ServerCertificateValidationCallback = null;

        HttpClientHelper.ConfigureCertificateValidation(true, out _, out _);

        if (Platform.IsNetCore)
        {
            Assert.Equal(SecurityProtocolType.Tls, ServicePointManager.SecurityProtocol);
            Assert.Null(ServicePointManager.ServerCertificateValidationCallback);
        }

        if (Platform.IsFullFramework)
        {
            Assert.Equal(SecurityProtocolType.Tls, ServicePointManager.SecurityProtocol);
            Assert.Null(ServicePointManager.ServerCertificateValidationCallback);
        }
    }

    [Fact]
    public void RestoreCertificateValidation_ValidateFalse()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;

        const RemoteCertificateValidationCallback prevValidator = null;
        var protocolType = SecurityProtocolType.Tls;

        HttpClientHelper.RestoreCertificateValidation(false, protocolType, prevValidator);

        if (Platform.IsNetCore)
        {
            Assert.Equal(SecurityProtocolType.Tls12, ServicePointManager.SecurityProtocol);
            Assert.NotNull(ServicePointManager.ServerCertificateValidationCallback);
        }

        if (Platform.IsFullFramework)
        {
            Assert.Equal(SecurityProtocolType.Tls, ServicePointManager.SecurityProtocol);
            Assert.Null(ServicePointManager.ServerCertificateValidationCallback);
        }
    }

    [Fact]
    public void RestoreCertificateValidation_ValidateTrue()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;

        const RemoteCertificateValidationCallback prevValidator = null;
        var protocolType = SecurityProtocolType.Tls;

        HttpClientHelper.RestoreCertificateValidation(true, protocolType, prevValidator);

        if (Platform.IsNetCore)
        {
            Assert.Equal(SecurityProtocolType.Tls12, ServicePointManager.SecurityProtocol);
            Assert.NotNull(ServicePointManager.ServerCertificateValidationCallback);
        }

        if (Platform.IsFullFramework)
        {
            Assert.Equal(SecurityProtocolType.Tls12, ServicePointManager.SecurityProtocol);
            Assert.NotNull(ServicePointManager.ServerCertificateValidationCallback);
        }
    }

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
    public void GetRequestMessage_ThrowsNulls()
    {
        Assert.Throws<ArgumentNullException>(() => HttpClientHelper.GetRequestMessage(null, null, null, null));
        Assert.Throws<ArgumentNullException>(() => HttpClientHelper.GetRequestMessage(HttpMethod.Get, null, null, null));
    }

    [Fact]
    public void GetRequestMessage_CreatesCorrectMessage()
    {
        HttpRequestMessage message = HttpClientHelper.GetRequestMessage(HttpMethod.Put, "https://localhost/foobar", null, null);
        Assert.NotNull(message);
        Assert.Equal(HttpMethod.Put, message.Method);
        Assert.Equal("https://localhost/foobar", message.RequestUri.ToString());
        Assert.Null(message.Headers.Authorization);
    }

    [Fact]
    public void GetRequestMessage_CreatesCorrectMessage_WithBasicAuth()
    {
        HttpRequestMessage message = HttpClientHelper.GetRequestMessage(HttpMethod.Put, "https://localhost/foobar", "foo", "bar");
        Assert.NotNull(message);
        Assert.Equal(HttpMethod.Put, message.Method);
        Assert.Equal("https://localhost/foobar", message.RequestUri.ToString());
        Assert.NotNull(message.Headers.Authorization);
        string bytes = Convert.ToBase64String(Encoding.ASCII.GetBytes("foo" + ":" + "bar"));
        Assert.Equal("Basic", message.Headers.Authorization.Scheme);
        Assert.Equal(bytes, message.Headers.Authorization.Parameter);
    }

    [Fact]
    public void GetAccessToken_ThrowsNulls()
    {
        Assert.ThrowsAsync<ArgumentException>(() => HttpClientHelper.GetAccessToken(string.Empty, null, null));
        Assert.ThrowsAsync<ArgumentException>(() => HttpClientHelper.GetAccessToken("https://foo/bar", null, null));
        Assert.ThrowsAsync<ArgumentException>(() => HttpClientHelper.GetAccessToken("https://foo/bar", "clientid", null));
    }

    [Fact]
    public void GetDisableDelegate_ReturnsExpected()
    {
        Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> del1 = HttpClientHelper.GetDisableDelegate();

        if (Platform.IsFullFramework)
        {
            Assert.Null(del1);
        }
        else
        {
            Assert.NotNull(del1);
        }
    }
}
