// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// This file is a modified version of https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/test/CertificateTests.cs

using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Security.Authentication.Mtls;
using Xunit;

namespace Steeltoe.Security.Authentication.MtlsCore.Test;

public class ClientCertificateAuthenticationTests
{
    private readonly CertificateAuthenticationEvents _successfulValidationEvents = new()
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

    private readonly CertificateAuthenticationEvents _failedValidationEvents = new()
    {
        OnCertificateValidated = context =>
        {
            context.Fail("Not validated");
            return Task.CompletedTask;
        }
    };

    private readonly CertificateAuthenticationEvents _unprocessedValidationEvents = new()
    {
        OnCertificateValidated = _ => Task.CompletedTask
    };

    [Fact]
    public async Task VerifySchemeDefaults()
    {
        var services = new ServiceCollection();
        services.AddAuthentication().AddMutualTls();
        ServiceProvider sp = services.BuildServiceProvider();
        var schemeProvider = sp.GetRequiredService<IAuthenticationSchemeProvider>();
        AuthenticationScheme scheme = await schemeProvider.GetSchemeAsync(CertificateAuthenticationDefaults.AuthenticationScheme);
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
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions(), Certificates.SelfSignedValidWithClientEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("http://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithClientEkuAuthenticates()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            Events = _successfulValidationEvents
        }, Certificates.SelfSignedValidWithClientEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithNoEkuAuthenticates()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            Events = _successfulValidationEvents
        }, Certificates.SelfSignedValidWithNoEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithClientEkuFailsWhenSelfSignedCertsNotAllowed()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.Chained
        }, Certificates.SelfSignedValidWithClientEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithNoEkuFailsWhenSelfSignedCertsNotAllowed()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.Chained,
            Events = _successfulValidationEvents
        }, Certificates.SelfSignedValidWithNoEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithServerFailsEvenIfSelfSignedCertsAreAllowed()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            Events = _successfulValidationEvents
        }, Certificates.SelfSignedValidWithServerEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithServerPassesWhenSelfSignedCertsAreAllowedAndPurposeValidationIsOff()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            ValidateCertificateUse = false,
            Events = _successfulValidationEvents
        }, Certificates.SelfSignedValidWithServerEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyValidSelfSignedWithServerFailsPurposeValidationIsOffButSelfSignedCertsAreNotAllowed()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.Chained,
            ValidateCertificateUse = false,
            Events = _successfulValidationEvents
        }, Certificates.SelfSignedValidWithServerEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    [Trait("Category", "SkipOnLinux")]
    public async Task VerifyExpiredSelfSignedFails()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            ValidateCertificateUse = false,
            Events = _successfulValidationEvents
        }, Certificates.SelfSignedExpired);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyExpiredSelfSignedPassesIfDateRangeValidationIsDisabled()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            ValidateValidityPeriod = false,
            Events = _successfulValidationEvents
        }, Certificates.SelfSignedExpired);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // https://github.com/dotnet/aspnetcore/issues/32813
    [Fact]
    [Trait("Category", "SkipOnLinux")]
    public async Task VerifyNotYetValidSelfSignedFails()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            ValidateCertificateUse = false,
            Events = _successfulValidationEvents
        }, Certificates.SelfSignedNotYetValid);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyNotYetValidSelfSignedPassesIfDateRangeValidationIsDisabled()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            ValidateValidityPeriod = false,
            Events = _successfulValidationEvents
        }, Certificates.SelfSignedNotYetValid);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyFailingInTheValidationEventReturnsForbidden()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            ValidateCertificateUse = false,
            Events = _failedValidationEvents
        }, Certificates.SelfSignedValidWithServerEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DoingNothingInTheValidationEventReturnsOk()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            ValidateCertificateUse = false,
            Events = _unprocessedValidationEvents
        }, Certificates.SelfSignedValidWithServerEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyNotSendingACertificateEndsUpInForbidden()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            Events = _successfulValidationEvents
        });

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyUntrustedClientCertEndsUpInForbidden()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            Events = _successfulValidationEvents
        }, Certificates.SignedClient);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifySideLoadedCaSignedCertReturnsOk()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            Events = _successfulValidationEvents,
            IssuerChain = new List<X509Certificate2>
            {
                Certificates.SelfSignedPrimaryRoot,
                Certificates.SignedSecondaryRoot
            }
        }, Certificates.SignedClient);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyHeaderIsUsedIfCertIsNotPresent()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            Events = _successfulValidationEvents
        }, wireUpHeaderMiddleware: true);

        HttpClient client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Cert", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
        HttpResponseMessage response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyHeaderEncodedCertFailsOnBadEncoding()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            Events = _successfulValidationEvents
        }, wireUpHeaderMiddleware: true);

        HttpClient client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Client-Cert", $"OOPS{Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData)}");
        HttpResponseMessage response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifySettingTheAzureHeaderOnTheForwarderOptionsWorks()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned,
            Events = _successfulValidationEvents
        }, wireUpHeaderMiddleware: true, headerName: "X-ARR-ClientCert");

        HttpClient client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-ARR-ClientCert", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
        HttpResponseMessage response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyACustomHeaderFailsIfTheHeaderIsNotPresent()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            Events = _successfulValidationEvents
        }, wireUpHeaderMiddleware: true, headerName: "X-ARR-ClientCert");

        HttpClient client = server.CreateClient();
        client.DefaultRequestHeaders.Add("random-Weird-header", Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData));
        HttpResponseMessage response = await client.GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyNoEventWireUpWithAValidCertificateCreatesADefaultUser()
    {
        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
        {
            AllowedCertificateTypes = CertificateTypes.SelfSigned
        }, Certificates.SelfSignedValidWithNoEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        XElement responseAsXml = null;

        if (response.Content != null && response.Content.Headers.ContentType != null && response.Content.Headers.ContentType.MediaType == "text/xml")
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            responseAsXml = XElement.Parse(responseContent);
        }

        Assert.NotNull(responseAsXml);

        // There should always be an Issuer and a Thumbprint.
        IEnumerable<XElement> actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == "issuer");
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
    public async Task VerifyValidationEventPrincipalIsPropagated()
    {
        const string expected = "John Doe";

        TestServer server = CreateServer(new MutualTlsAuthenticationOptions
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
        }, Certificates.SelfSignedValidWithNoEku);

        HttpResponseMessage response = await server.CreateClient().GetAsync("https://example.com/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        XElement responseAsXml = null;

        if (response.Content != null && response.Content.Headers.ContentType != null && response.Content.Headers.ContentType.MediaType == "text/xml")
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            responseAsXml = XElement.Parse(responseContent);
        }

        Assert.NotNull(responseAsXml);
        IEnumerable<XElement> actual = responseAsXml.Elements("claim").Where(claim => claim.Attribute("Type").Value == ClaimTypes.Name);
        Assert.Single(actual);
        Assert.Equal(expected, actual.First().Value);
        Assert.Single(responseAsXml.Elements("claim"));
    }

    private static TestServer CreateServer(MutualTlsAuthenticationOptions configureOptions, X509Certificate2 clientCertificate = null, Uri baseAddress = null,
        bool wireUpHeaderMiddleware = false, string headerName = "")
    {
        IWebHostBuilder builder = new WebHostBuilder().Configure(app =>
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
                HttpResponse response = context.Response;

                AuthenticateResult authenticationResult = await context.AuthenticateAsync();

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
        }).ConfigureServices(services =>
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

    private static class Certificates
    {
        private static readonly string ServerEku = "1.3.6.1.5.5.7.3.1";
        private static readonly string ClientEku = "1.3.6.1.5.5.7.3.2";

        private static readonly X509KeyUsageExtension DigitalSignatureOnlyUsage = new(X509KeyUsageFlags.DigitalSignature, true);

        public static X509Certificate2 SelfSignedPrimaryRoot { get; }

        public static X509Certificate2 SignedSecondaryRoot { get; }

        public static X509Certificate2 SignedClient { get; }

        public static X509Certificate2 SelfSignedValidWithClientEku { get; }

        public static X509Certificate2 SelfSignedValidWithNoEku { get; }

        public static X509Certificate2 SelfSignedValidWithServerEku { get; }

        public static X509Certificate2 SelfSignedNotYetValid { get; }

        public static X509Certificate2 SelfSignedExpired { get; }

        static Certificates()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            SelfSignedPrimaryRoot = MakeCert("CN=Valid Self Signed Client EKU,OU=dev,DC=idunno-dev,DC=org", ClientEku, now);

            SignedSecondaryRoot = MakeCert("CN=Valid Signed Secondary Root EKU,OU=dev,DC=idunno-dev,DC=org", ClientEku, now);

            SelfSignedValidWithServerEku = MakeCert("CN=Valid Self Signed Server EKU,OU=dev,DC=idunno-dev,DC=org", ServerEku, now);

            SelfSignedValidWithClientEku = MakeCert("CN=Valid Self Signed Server EKU,OU=dev,DC=idunno-dev,DC=org", ClientEku, now);

            SelfSignedValidWithNoEku = MakeCert("CN=Valid Self Signed No EKU,OU=dev,DC=idunno-dev,DC=org", null, now);

            SelfSignedExpired = MakeCert("CN=Expired Self Signed,OU=dev,DC=idunno-dev,DC=org", null, now.AddYears(-2), now.AddYears(-1));

            SelfSignedNotYetValid = MakeCert("CN=Not Valid Yet Self Signed,OU=dev,DC=idunno-dev,DC=org", null, now.AddYears(2), now.AddYears(3));

            SignedClient = MakeCert("CN=Valid Signed Client,OU=dev,DC=idunno-dev,DC=org", ClientEku, now);
        }

        private static X509Certificate2 MakeCert(string subjectName, string eku, DateTimeOffset now)
        {
            return MakeCert(subjectName, eku, now, now.AddYears(5));
        }

        private static X509Certificate2 MakeCert(string subjectName, string eku, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest(subjectName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(DigitalSignatureOnlyUsage);

            if (eku != null)
            {
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
                {
                    new(eku, null)
                }, false));
            }

            return request.CreateSelfSigned(notBefore, notAfter);
        }
    }
}
