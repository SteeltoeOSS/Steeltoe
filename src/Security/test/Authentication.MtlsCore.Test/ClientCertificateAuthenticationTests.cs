// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// This file is a modified version of https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/test/CertificateTests.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Security.Authentication.Mtls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Steeltoe.Security.Authentication.MtlsCore.Test;

public class ClientCertificateAuthenticationTests
{
    [Fact]
    public async Task VerifySchemeDefaults()
    {
        var services = new ServiceCollection();
        services.AddAuthentication().AddMutualTls();
        var sp = services.BuildServiceProvider();
        var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetSchemeAsync(CertificateAuthenticationDefaults.AuthenticationScheme);
        Assert.NotNull(scheme);
        Assert.Equal("MutualTlsAuthenticationHandler", scheme.HandlerType.Name);
        Assert.Null(scheme.DisplayName);
    }

    [Fact]
    public void VerifyIsSelfSignedExtensionMethod()
    {
        Assert.True(Certificates.SelfSignedValidWithNoEku.IsSelfSigned());
    }

    [Fact]
    public async Task NonHttpsIsForbidden()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions(),
            Certificates.SelfSignedValidWithClientEku);

        var response = await server.CreateClient().GetAsync("http://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithClientEkuAuthenticates()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = _successfulValidationEvents
            },
            Certificates.SelfSignedValidWithClientEku);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithNoEkuAuthenticates()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = _successfulValidationEvents
            },
            Certificates.SelfSignedValidWithNoEku);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithClientEkuFailsWhenSelfSignedCertsNotAllowed()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
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
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.Chained,
                Events = _successfulValidationEvents
            },
            Certificates.SelfSignedValidWithNoEku);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithServerFailsEvenIfSelfSignedCertsAreAllowed()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = _successfulValidationEvents
            },
            Certificates.SelfSignedValidWithServerEku);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithServerPassesWhenSelfSignedCertsAreAllowedAndPurposeValidationIsOff()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateCertificateUse = false,
                Events = _successfulValidationEvents
            },
            Certificates.SelfSignedValidWithServerEku);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithServerFailsPurposeValidationIsOffButSelfSignedCertsAreNotAllowed()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.Chained,
                ValidateCertificateUse = false,
                Events = _successfulValidationEvents
            },
            Certificates.SelfSignedValidWithServerEku);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "SkipOnLinux")]
    public async Task VerifyExpiredSelfSignedFails()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateCertificateUse = false,
                Events = _successfulValidationEvents
            },
            Certificates.SelfSignedExpired);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyExpiredSelfSignedPassesIfDateRangeValidationIsDisabled()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateValidityPeriod = false,
                Events = _successfulValidationEvents
            },
            Certificates.SelfSignedExpired);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // https://github.com/dotnet/aspnetcore/issues/32813
    [Fact]
    [Trait("Category", "SkipOnLinux")]
    public async Task VerifyNotYetValidSelfSignedFails()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateCertificateUse = false,
                Events = _successfulValidationEvents
            },
            Certificates.SelfSignedNotYetValid);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyNotYetValidSelfSignedPassesIfDateRangeValidationIsDisabled()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateValidityPeriod = false,
                Events = _successfulValidationEvents
            },
            Certificates.SelfSignedNotYetValid);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyFailingInTheValidationEventReturnsForbidden()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                ValidateCertificateUse = false,
                Events = _failedValidationEvents
            },
            Certificates.SelfSignedValidWithServerEku);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DoingNothingInTheValidationEventReturnsOk()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                ValidateCertificateUse = false,
                Events = _unprocessedValidationEvents
            },
            Certificates.SelfSignedValidWithServerEku);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyNotSendingACertificateEndsUpInForbidden()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                Events = _successfulValidationEvents
            });

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyUntrustedClientCertEndsUpInForbidden()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                Events = _successfulValidationEvents
            }, Certificates.SignedClient);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifySideloadedCaSignedCertReturnsOk()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = _successfulValidationEvents,
                IssuerChain = new List<X509Certificate2> { Certificates.SelfSignedPrimaryRoot, Certificates.SignedSecondaryRoot }
            }, Certificates.SignedClient);

        var response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyHeaderIsUsedIfCertIsNotPresent()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = _successfulValidationEvents
            },
            wireUpHeaderMiddleware: true);

        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Cert", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
        var response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyHeaderEncodedCertFailsOnBadEncoding()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                Events = _successfulValidationEvents
            },
            wireUpHeaderMiddleware: true);

        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Cert", $"OOPS{Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData)}");
        var response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifySettingTheAzureHeaderOnTheForwarderOptionsWorks()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = _successfulValidationEvents
            },
            wireUpHeaderMiddleware: true,
            headerName: "X-ARR-ClientCert");

        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-ARR-ClientCert", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
        var response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyACustomHeaderFailsIfTheHeaderIsNotPresent()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                Events = _successfulValidationEvents
            },
            wireUpHeaderMiddleware: true,
            headerName: "X-ARR-ClientCert");

        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("random-Weird-header", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
        var response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyNoEventWireupWithAValidCertificateCreatesADefaultUser()
    {
        var server = CreateServer(
            new MutualTlsAuthenticationOptions
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
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.SubjectName.Name, actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.SerialNumber))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.SerialNumber);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.SerialNumber, actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.DnsName, false)))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Dns);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.DnsName, false), actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.EmailName, false)))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Email);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.EmailName, false), actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.SimpleName, false)))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.SimpleName, false), actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UpnName, false)))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Upn);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UpnName, false), actual.First().Value);
            }
        }

        if (!string.IsNullOrEmpty(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UrlName, false)))
        {
            actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Uri);
            if (actual.Any())
            {
                Assert.Single(actual);
                Assert.Equal(Certificates.SelfSignedValidWithNoEku.GetNameInfo(X509NameType.UrlName, false), actual.First().Value);
            }
        }
    }

    [Fact]
    public async Task VerifyValidationEventPrincipalIsPropogated()
    {
        const string expected = "John Doe";

        var server = CreateServer(
            new MutualTlsAuthenticationOptions
            {
                AllowedCertificateTypes = CertificateTypes.SelfSigned,
                Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        // Make sure we get the validated principal
                        Assert.NotNull(context.Principal);
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.Name, expected, ClaimValueTypes.String, context.Options.ClaimsIssuer)
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
        Assert.Equal(expected, actual.First().Value);
        Assert.Single(responseAsXml.Elements("claim"));
    }

    private static TestServer CreateServer(
        MutualTlsAuthenticationOptions configureOptions,
        X509Certificate2 clientCertificate = null,
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
                    app.UseCertificateForwarding();
                }

                app.UseAuthentication();

                app.Run(async context =>
                {
                    var response = context.Response;

                    var authenticationResult = await context.AuthenticateAsync();

                    if (authenticationResult.Succeeded)
                    {
                        response.StatusCode = (int)HttpStatusCode.OK;
                        response.ContentType = "text/xml";

                        await response.WriteAsync("<claims>");
                        foreach (var claim in context.User.Claims)
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
                    services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddMutualTls(options =>
                    {
                        options.AllowedCertificateTypes = configureOptions.AllowedCertificateTypes;
                        options.Events = configureOptions.Events;
                        options.ValidateCertificateUse = configureOptions.ValidateCertificateUse;
                        options.RevocationFlag = configureOptions.RevocationFlag;
                        options.RevocationMode = configureOptions.RevocationMode;
                        options.ValidateValidityPeriod = configureOptions.ValidateValidityPeriod;
                        options.IssuerChain = configureOptions.IssuerChain;
                    });
                }
                else
                {
                    services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate();
                }

                if (wireUpHeaderMiddleware && !string.IsNullOrEmpty(headerName))
                {
                    services.AddCertificateForwarding(options =>
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

    private readonly CertificateAuthenticationEvents _successfulValidationEvents = new ()
    {
        OnCertificateValidated = context =>
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

    private readonly CertificateAuthenticationEvents _failedValidationEvents = new ()
    {
        OnCertificateValidated = context =>
        {
            context.Fail("Not validated");
            return Task.CompletedTask;
        }
    };

    private readonly CertificateAuthenticationEvents _unprocessedValidationEvents = new ()
    {
        OnCertificateValidated = _ => Task.CompletedTask
    };

    private static class Certificates
    {
        private static readonly string ServerEku = "1.3.6.1.5.5.7.3.1";
        private static readonly string ClientEku = "1.3.6.1.5.5.7.3.2";

        static Certificates()
        {
            var now = DateTimeOffset.UtcNow;

            SelfSignedPrimaryRoot = MakeCert(
                "CN=Valid Self Signed Client EKU,OU=dev,DC=idunno-dev,DC=org",
                ClientEku,
                now);

            SignedSecondaryRoot = MakeCert(
                "CN=Valid Signed Secondary Root EKU,OU=dev,DC=idunno-dev,DC=org",
                ClientEku,
                now);

            SelfSignedValidWithServerEku = MakeCert(
                "CN=Valid Self Signed Server EKU,OU=dev,DC=idunno-dev,DC=org",
                ServerEku,
                now);

            SelfSignedValidWithClientEku = MakeCert(
                "CN=Valid Self Signed Server EKU,OU=dev,DC=idunno-dev,DC=org",
                ClientEku,
                now);

            SelfSignedValidWithNoEku = MakeCert(
                "CN=Valid Self Signed No EKU,OU=dev,DC=idunno-dev,DC=org",
                eku: null,
                now);

            SelfSignedExpired = MakeCert(
                "CN=Expired Self Signed,OU=dev,DC=idunno-dev,DC=org",
                eku: null,
                now.AddYears(-2),
                now.AddYears(-1));

            SelfSignedNotYetValid = MakeCert(
                "CN=Not Valid Yet Self Signed,OU=dev,DC=idunno-dev,DC=org",
                eku: null,
                now.AddYears(2),
                now.AddYears(3));

            SignedClient = MakeCert(
                "CN=Valid Signed Client,OU=dev,DC=idunno-dev,DC=org",
                ClientEku,
                now);
        }

        private static readonly X509KeyUsageExtension DigitalSignatureOnlyUsage =
            new (X509KeyUsageFlags.DigitalSignature, true);

        private static X509Certificate2 MakeCert(
            string subjectName,
            string eku,
            DateTimeOffset now)
        {
            return MakeCert(subjectName, eku, now, now.AddYears(5));
        }

        private static X509Certificate2 MakeCert(
            string subjectName,
            string eku,
            DateTimeOffset notBefore,
            DateTimeOffset notAfter)
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest(
                subjectName,
                key,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(DigitalSignatureOnlyUsage);

            if (eku != null)
            {
                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection { new (eku, null) }, false));
            }

            return request.CreateSelfSigned(notBefore, notAfter);
        }

        public static X509Certificate2 SelfSignedPrimaryRoot { get; private set; }

        public static X509Certificate2 SignedSecondaryRoot { get; private set; }

        public static X509Certificate2 SignedClient { get; private set; }

        public static X509Certificate2 SelfSignedValidWithClientEku { get; private set; }

        public static X509Certificate2 SelfSignedValidWithNoEku { get; private set; }

        public static X509Certificate2 SelfSignedValidWithServerEku { get; private set; }

        public static X509Certificate2 SelfSignedNotYetValid { get; private set; }

        public static X509Certificate2 SelfSignedExpired { get; private set; }
    }
}
