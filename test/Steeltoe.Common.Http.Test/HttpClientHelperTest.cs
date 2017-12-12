// Copyright 2017 the original author or authors.
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

using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using Xunit;

namespace Steeltoe.Common.Http.Test
{
    public class HttpClientHelperTest
    {
        [Fact]
        public void GetHttpClient_SetsTimeout()
        {
            var client = HttpClientHelper.GetHttpClient(false, 100);
            Assert.Equal(100, client.Timeout.TotalMilliseconds);
        }

        [Fact]
        public void ConfigureCertificateValidatation_ValidateFalse()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = null;

            RemoteCertificateValidationCallback prevValidator;
            SecurityProtocolType protocolType;

            HttpClientHelper.ConfigureCertificateValidatation(false, out protocolType, out prevValidator);

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
        public void ConfigureCertificateValidatation_ValidateTrue()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = null;

            RemoteCertificateValidationCallback prevValidator;
            SecurityProtocolType protocolType;

            HttpClientHelper.ConfigureCertificateValidatation(true, out protocolType, out prevValidator);

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
            SecurityProtocolType protocolType = SecurityProtocolType.Tls;

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
            SecurityProtocolType protocolType = SecurityProtocolType.Tls;

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
            Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Empty + ":" + string.Empty)), result);

            string result2 = HttpClientHelper.GetEncodedUserPassword("foo", null);
            Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes("foo" + ":" + string.Empty)), result2);

            string result3 = HttpClientHelper.GetEncodedUserPassword(null, "bar");
            Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Empty + ":" + "bar")), result3);
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
            var message = HttpClientHelper.GetRequestMessage(HttpMethod.Put, "http://localhost/foobar", null, null);
            Assert.NotNull(message);
            Assert.Equal(HttpMethod.Put, message.Method);
            Assert.Equal("http://localhost/foobar", message.RequestUri.ToString());
            Assert.Null(message.Headers.Authorization);
        }

        [Fact]
        public void GetRequestMessage_CreatesCorrectMessage_WithBasicAuth()
        {
            var message = HttpClientHelper.GetRequestMessage(HttpMethod.Put, "http://localhost/foobar", "foo", "bar");
            Assert.NotNull(message);
            Assert.Equal(HttpMethod.Put, message.Method);
            Assert.Equal("http://localhost/foobar", message.RequestUri.ToString());
            Assert.NotNull(message.Headers.Authorization);
            var bytes = Convert.ToBase64String(Encoding.ASCII.GetBytes("foo" + ":" + "bar"));
            Assert.Equal("Basic", message.Headers.Authorization.Scheme);
            Assert.Equal(bytes, message.Headers.Authorization.Parameter);
        }

        [Fact]
        public void GetAccessToken_ThrowsNulls()
        {
            Assert.ThrowsAsync<ArgumentException>(() => HttpClientHelper.GetAccessToken(null, null, null));
            Assert.ThrowsAsync<ArgumentException>(() => HttpClientHelper.GetAccessToken("http://foo/bar", null, null));
            Assert.ThrowsAsync<ArgumentException>(() => HttpClientHelper.GetAccessToken("http://foo/bar", "clientid", null));
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
}
