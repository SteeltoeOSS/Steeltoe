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

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Security;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class CloudFoundryContainerIdentityMtlsTest : IClassFixture<ClientCertificatesFixture>
    {
        private readonly ClientCertificatesFixture fixture;

        public CloudFoundryContainerIdentityMtlsTest(ClientCertificatesFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async void CloudFoundryCertificateAuth_AcceptsSameSpace()
        {
            // arrange
            var host = await GetHostBuilder().StartAsync();

            // act
            var response = await ClientWithCertificate(host.GetTestClient(), Certificates.OrgAndSpaceMatch).GetAsync("https://localhost/" + CloudFoundryDefaults.SameSpaceAuthorizationPolicy);

            // assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async void CloudFoundryCertificateAuth_AcceptsSameOrg()
        {
            // arrange
            var host = await GetHostBuilder().StartAsync();

            // act
            var response = await ClientWithCertificate(host.GetTestClient(), Certificates.OrgAndSpaceMatch).GetAsync("https://localhost/" + CloudFoundryDefaults.SameOrganizationAuthorizationPolicy);

            // assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async void CloudFoundryCertificateAuth_RejectsOrgMismatch()
        {
            // arrange
            var host = await GetHostBuilder().StartAsync();

            // act
            var response = await ClientWithCertificate(host.GetTestClient(), Certificates.SpaceMatch).GetAsync("https://localhost/" + CloudFoundryDefaults.SameOrganizationAuthorizationPolicy);

            // assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async void CloudFoundryCertificateAuth_RejectsSpaceMismatch()
        {
            // arrange
            var host = await GetHostBuilder().StartAsync();

            // act
            var response = await ClientWithCertificate(host.GetTestClient(), Certificates.OrgMatch).GetAsync("https://localhost/" + CloudFoundryDefaults.SameSpaceAuthorizationPolicy);

            // assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async void AddCloudFoundryCertificateAuth_ForbiddenWithoutCert()
        {
            // arrange
            var host = await GetHostBuilder().StartAsync();

            // act
            var response = await host.GetTestClient().GetAsync("http://localhost/" + CloudFoundryDefaults.SameSpaceAuthorizationPolicy);

            // assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private IHostBuilder GetHostBuilder()
        {
            return new HostBuilder()
                .ConfigureAppConfiguration(cfg => cfg.AddCloudFoundryContainerIdentity(fixture.ServerOrgId.ToString(), fixture.ServerSpaceId.ToString()))
                .ConfigureWebHostDefaults(webHost => webHost.UseStartup<TestServerCertificateStartup>())
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                });
        }

        private HttpClient ClientWithCertificate(HttpClient httpClient, X509Certificate2 certificate)
        {
            var bytes = certificate.GetRawCertData();
            var b64 = Convert.ToBase64String(bytes);
            httpClient.DefaultRequestHeaders.Add("X-Forwarded-Client-Cert", b64);
            return httpClient;
        }

        private static class Certificates
        {
            public static X509Certificate2 OrgAndSpaceMatch { get; } =
                new X509Certificate2(GetFullyQualifiedFilePath("OrgAndSpaceMatchCert.pem"))
                .CopyWithPrivateKey(PemConfigureCertificateOptions.ReadRsaKeyFromString(File.ReadAllText(GetFullyQualifiedFilePath("OrgAndSpaceMatchKey.pem"))));

            public static X509Certificate2 OrgMatch { get; } =
                new X509Certificate2(GetFullyQualifiedFilePath("OrgMatchCert.pem"))
                .CopyWithPrivateKey(PemConfigureCertificateOptions.ReadRsaKeyFromString(File.ReadAllText(GetFullyQualifiedFilePath("OrgMatchKey.pem"))));

            public static X509Certificate2 SpaceMatch { get; } =
                new X509Certificate2(GetFullyQualifiedFilePath("SpaceMatchCert.pem"))
                .CopyWithPrivateKey(PemConfigureCertificateOptions.ReadRsaKeyFromString(File.ReadAllText(GetFullyQualifiedFilePath("SpaceMatchKey.pem"))));

            private static string GetFullyQualifiedFilePath(string filename)
            {
                var filePath = Path.Combine(LocalCertificateWriter.AppBasePath, "GeneratedCertificates", filename);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException(filePath);
                }

                return filePath;
            }
        }
    }
}