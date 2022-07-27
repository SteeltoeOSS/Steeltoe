// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Http.Test;

public class HttpClientHelperTest
{
    [Fact]
    public void GetHttpClient_SetsTimeout()
    {
        var client = HttpClientHelper.GetHttpClient(false, 100);
        Assert.Equal(100, client.Timeout.TotalMilliseconds);
    }

    [Fact]
    public void ConfigureCertificateValidation_ValidateFalse()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
        ServicePointManager.ServerCertificateValidationCallback = null;

        HttpClientHelper.ConfigureCertificateValidation(false, out var protocolType, out var prevValidator);

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

        HttpClientHelper.ConfigureCertificateValidation(true, out var protocolType, out var prevValidator);

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
        ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

        RemoteCertificateValidationCallback prevValidator = null;
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
        ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

        RemoteCertificateValidationCallback prevValidator = null;
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
        var result = HttpClientHelper.GetEncodedUserPassword(null, null);
        Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Empty + ":" + string.Empty)), result);

        var result2 = HttpClientHelper.GetEncodedUserPassword("foo", null);
        Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes("foo" + ":" + string.Empty)), result2);

        var result3 = HttpClientHelper.GetEncodedUserPassword(null, "bar");
        Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Empty + ":" + "bar")), result3);
    }

    [Fact]
    public void GetEncodedUserPassword_NotNulls()
    {
        var result = HttpClientHelper.GetEncodedUserPassword("foo", "bar");
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
        var message = HttpClientHelper.GetRequestMessage(HttpMethod.Put, "https://localhost/foobar", null, null);
        Assert.NotNull(message);
        Assert.Equal(HttpMethod.Put, message.Method);
        Assert.Equal("https://localhost/foobar", message.RequestUri.ToString());
        Assert.Null(message.Headers.Authorization);
    }

    [Fact]
    public void GetRequestMessage_CreatesCorrectMessage_WithBasicAuth()
    {
        var message = HttpClientHelper.GetRequestMessage(HttpMethod.Put, "https://localhost/foobar", "foo", "bar");
        Assert.NotNull(message);
        Assert.Equal(HttpMethod.Put, message.Method);
        Assert.Equal("https://localhost/foobar", message.RequestUri.ToString());
        Assert.NotNull(message.Headers.Authorization);
        var bytes = Convert.ToBase64String(Encoding.ASCII.GetBytes("foo" + ":" + "bar"));
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
        var del1 = HttpClientHelper.GetDisableDelegate();

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