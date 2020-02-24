﻿// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Security.Authentication.MtlsCore.Events;
using Xunit;

namespace Steeltoe.Security.Authentication.MtlsCore.Test
{
    public class ClientCertificateAuthenticationTests
    {
        [Fact]
        public void CheckThatTestRootCAIsLoaded()
        {
            bool found;

            using (var rootCAStore = new X509Store(StoreName.Root))
            {
                rootCAStore.Open(OpenFlags.ReadOnly);

                var certificates = rootCAStore.Certificates.Find(
                    X509FindType.FindBySerialNumber,
                    "5d452c99003e54954f85aca776fd5b2c",
                    true);

                found = certificates.Count != 0;

                rootCAStore.Close();
            }

            Assert.True(found);
        }

        [Fact]
        public async Task VerifySchemeDefaults()
        {
            var services = new ServiceCollection();
            services.AddAuthentication().AddCertificate();
            var sp = services.BuildServiceProvider();
            var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemeProvider.GetSchemeAsync(CertificateAuthenticationDefaults.AuthenticationScheme);
            Assert.NotNull(scheme);
            Assert.Equal("CertificateAuthenticationHandler", scheme.HandlerType.Name);
            Assert.Null(scheme.DisplayName);
        }

        [Fact]
        public void VerifyIsSelfSignedExtensionMethod()
        {
            Assert.True(Certificates.SelfSignedValidWithNoEku.IsSelfSigned());
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithClientEkuAuthenticates()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    Events = sucessfulValidationEvents
                },
                Certificates.SelfSignedValidWithClientEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithNoEkuAuthenticates()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    Events = sucessfulValidationEvents
                },
                Certificates.SelfSignedValidWithNoEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithClientEkuFailsWhenSelfSignedCertsNotAllowed()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.Chained
                },
                Certificates.SelfSignedValidWithClientEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithNoEkuFailsWhenSelfSignedCertsNotAllowed()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.Chained,
                    Events = sucessfulValidationEvents
                },
                Certificates.SelfSignedValidWithNoEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithServerFailsEvenIfSelfSignedCertsAreAllowed()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    Events = sucessfulValidationEvents
                },
                Certificates.SelfSignedValidWithServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithServerPassesWhenSelfSignedCertsAreAllowedAndPurposeValidationIsOff()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateCertificateUse = false,
                    Events = sucessfulValidationEvents
                },
                Certificates.SelfSignedValidWithServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyValidSelfSignedWithServerFailsPurposeValidationIsOffButSelfSignedCertsAreNotAllowed()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.Chained,
                    ValidateCertificateUse = false,
                    Events = sucessfulValidationEvents
                },
                Certificates.SelfSignedValidWithServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyExpiredSelfSignedFails()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateCertificateUse = false,
                    Events = sucessfulValidationEvents
                },
                Certificates.SelfSignedExpired);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyExpiredSelfSignedPassesIfDateRangeValidationIsDisabled()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateValidityPeriod = false,
                    Events = sucessfulValidationEvents
                },
                Certificates.SelfSignedExpired);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyNotYetValidSelfSignedFails()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateCertificateUse = false,
                    Events = sucessfulValidationEvents
                },
                Certificates.SelfSignedNotYetValid);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyNotYetValidSelfSignedPassesIfDateRangeValidationIsDisabled()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    ValidateValidityPeriod = false,
                    Events = sucessfulValidationEvents
                },
                Certificates.SelfSignedNotYetValid);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyRootedCertWithNoEkuPassesByDefault()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = sucessfulValidationEvents
                },
                Certificates.RootedNoEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyRootedCertWithClientEkuPassesByDefault()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = sucessfulValidationEvents
                },
                Certificates.RootedClientEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyRootedCertWithServerEkuFailsByDefault()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = sucessfulValidationEvents
                },
                Certificates.RootedServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyRootedCertWithServerEkuPassesIfEkuValidationIsTurnedOff()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    ValidateCertificateUse = false,
                    Events = sucessfulValidationEvents
                },
                Certificates.RootedServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyRevokedCertFailsByDefault()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = sucessfulValidationEvents
                },
                Certificates.RootedRevoked);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyFailingInTheValidationEventReturnsForbidden()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    ValidateCertificateUse = false,
                    Events = failedValidationEvents
                },
                Certificates.RootedServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task DoingNothingInTheValidationEventReturnsOK()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    ValidateCertificateUse = false,
                    Events = unprocessedValidationEvents
                },
                Certificates.RootedServerEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyNotSendingACertificateEndsUpInForbidden()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = sucessfulValidationEvents
                });

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyArrHeaderIsUsedIfCertIsNotPresent()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = sucessfulValidationEvents
                },
                wireUpHeaderMiddleware : true);

            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("X-ARR-ClientCert", Convert.ToBase64String(Certificates.RootedNoEku.RawData));
            var response = await client.GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyArrHeaderEncodedCertFailsOnBadEncoding()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = sucessfulValidationEvents
                },
                wireUpHeaderMiddleware: true);

            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("X-ARR-ClientCert", "OOPS" + Convert.ToBase64String(Certificates.RootedNoEku.RawData));
            var response = await client.GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifySettingTheHeaderOnTheForwarderOptionsWorks()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = sucessfulValidationEvents
                },
                wireUpHeaderMiddleware: true,
                headerName: "random-Weird-header");

            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("random-Weird-header", Convert.ToBase64String(Certificates.RootedNoEku.RawData));
            var response = await client.GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyACustomHeaderFailsIfTheHeaderIsNotPresent()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    Events = sucessfulValidationEvents
                },
                wireUpHeaderMiddleware: true,
                headerName: "another-random-Weird-header");

            var client = server.CreateClient();
            client.DefaultRequestHeaders.Add("random-Weird-header", Convert.ToBase64String(Certificates.RootedNoEku.RawData));
            var response = await client.GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task VerifyNoEventWireupWithAValidCertificateCreatesADefaultUser()
        {
            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned
                },
                Certificates.SelfSignedValidWithNoEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            XElement responseAsXml = null;
            if (response.Content != null &&
                response.Content.Headers.ContentType != null &&
                response.Content.Headers.ContentType.MediaType == "text/xml")
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                responseAsXml = XElement.Parse(responseContent);
            }

            Assert.NotNull(responseAsXml);

            // There should always be an Issuer and a Thumbprint.
            var actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == "issuer");
            Assert.Single(actual);
            Assert.Equal(Certificates.SelfSignedValidWithNoEku.Issuer, actual.First().Value);

            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Thumbprint);
            Assert.Single(actual);
            Assert.Equal(Certificates.SelfSignedValidWithNoEku.Thumbprint, actual.First().Value);

            // Now the optional ones
            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.SubjectName.Name))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.X500DistinguishedName);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.SubjectName.Name, actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.SerialNumber))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.SerialNumber);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.SerialNumber, actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.DnsName, false)))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Dns);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.DnsName, false), actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.EmailName, false)))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Email);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.EmailName, false), actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.SimpleName, false)))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.SimpleName, false), actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UpnName, false)))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Upn);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UpnName, false), actual.First().Value);
                }
            }

            if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UrlName, false)))
            {
                actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Uri);
                if (actual.Count() > 0)
                {
                    Assert.Single(actual);
                    Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UrlName, false), actual.First().Value);
                }
            }
        }

        [Fact]
        public async Task VerifyValidationEventPrincipalIsPropogated()
        {
            const string Expected = "John Doe";

            var server = CreateServer(
                new CertificateAuthenticationOptions
                {
                    AllowedCertificateTypes = CertificateTypes.SelfSigned,
                    Events = new CertificateAuthenticationEvents
                    {
                        OnValidateCertificate = context =>
                        {
                            var claims = new[]
                            {
                                new Claim(ClaimTypes.Name, Expected, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                            };

                            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                            context.Success();
                            return Task.CompletedTask;
                        }
                    }
                },
                Certificates.SelfSignedValidWithNoEku);

            var response = await server.CreateClient().GetAsync("https://example.com/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            XElement responseAsXml = null;
            if (response.Content != null &&
                response.Content.Headers.ContentType != null &&
                response.Content.Headers.ContentType.MediaType == "text/xml")
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                responseAsXml = XElement.Parse(responseContent);
            }

            Assert.NotNull(responseAsXml);
            var actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
            Assert.Single(actual);
            Assert.Equal(Expected, actual.First().Value);
            Assert.Single(responseAsXml.Elements("claim"));
        }

        public static TestServer CreateServer(
            CertificateAuthenticationOptions configureOptions,
            X509Certificate2 clientCertificate = null,
            Func<HttpContext, bool> handler = null,
            Uri baseAddress = null,
            bool wireUpHeaderMiddleware = false,
            string headerName = "")
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Use((context, next) =>
                    {
                        if (clientCertificate != null)
                        {
                            context.Connection.ClientCertificate = clientCertificate;
                        }
                        return next();
                    });


                    if (wireUpHeaderMiddleware)
                    {
                        app.UseCertificateHeaderForwarding();
                    }

                    app.UseAuthentication();

                    app.Use(async (context, next) =>
                    {
                        var request = context.Request;
                        var response = context.Response;

                        var authenticationResult = await context.AuthenticateAsync();

                        if (authenticationResult.Succeeded)
                        {
                            response.StatusCode = (int)HttpStatusCode.OK;
                            response.ContentType = "text/xml";

                            await response.WriteAsync("<claims>");
                            foreach (Claim claim in context.User.Claims)
                            {
                                await response.WriteAsync($"<claim Type=\"{claim.Type}\" Issuer=\"{claim.Issuer}\">{claim.Value}</claim>");
                            }
                            await response.WriteAsync("</claims>");
                        }
                        else
                        {
                            await context.ChallengeAsync();
                        }
                    });
                })
            .ConfigureServices(services =>
            {
                if (configureOptions != null)
                {
                    services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate(options =>
                    {
                        options.AllowedCertificateTypes = configureOptions.AllowedCertificateTypes;
                        options.Events = configureOptions.Events;
                        options.ValidateCertificateUse = configureOptions.ValidateCertificateUse;
                        options.RevocationFlag = options.RevocationFlag;
                        options.RevocationMode = options.RevocationMode;
                        options.ValidateValidityPeriod = configureOptions.ValidateValidityPeriod;
                    });
                }
                else
                {
                    services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate();
                }

                if (wireUpHeaderMiddleware && !string.IsNullOrEmpty(headerName))
                {
                    services.AddCertificateHeaderForwarding(options =>
                    {
                        options.CertificateHeader = headerName;
                    });
                }
            });

            var server = new TestServer(builder)
            {
                BaseAddress = baseAddress
            };

            return server;
        }

        private CertificateAuthenticationEvents sucessfulValidationEvents = new CertificateAuthenticationEvents()
        {
            OnValidateCertificate = context =>
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, context.ClientCertificate.Subject, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                    new Claim(ClaimTypes.Name, context.ClientCertificate.Subject, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                };

                context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                context.Success();
                return Task.CompletedTask;
            }
        };

        private CertificateAuthenticationEvents failedValidationEvents = new CertificateAuthenticationEvents()
        {
            OnValidateCertificate = context =>
            {
                context.Fail("Not validated");
                return Task.CompletedTask;
            }
        };

        private CertificateAuthenticationEvents unprocessedValidationEvents = new CertificateAuthenticationEvents()
        {
            OnValidateCertificate = context =>
            {
                return Task.CompletedTask;
            }
        };

        private static class Certificates
        {
            private static readonly string collateralPath =
                Path.Combine(Directory.GetCurrentDirectory(), "TestCertificates");

            public static X509Certificate2 SelfSignedValidWithClientEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSelfSignedClientEkuCertificate.cer"));

            public static X509Certificate2 SelfSignedValidWithNoEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSelfSignedNoEkuCertificate.cer"));

            public static X509Certificate2 SelfSignedValidWithServerEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSelfSignedServerEkuCertificate.cer"));

            public static X509Certificate2 SelfSignedNotYetValid { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("selfSignedNoEkuCertificateNotValidYet.cer"));

            public static X509Certificate2 SelfSignedExpired { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("selfSignedNoEkuCertificateExpired.cer"));

            public static X509Certificate2 RootedNoEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("rootedNoEku.cer"));

            public static X509Certificate2 RootedClientEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("rootedClientEku.cer"));

            public static X509Certificate2 RootedServerEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("rootedServerEku.cer"));

            public static X509Certificate2 RootedRevoked { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("rootedRevoked.cer"));

            private static string GetFullyQualifiedFilePath(string filename)
            {
                var filePath = Path.Combine(collateralPath, filename);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException(filePath);
                }
                return filePath;
            }
        }
    }
}
