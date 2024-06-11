// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// This file is a modified version of https://github.com/dotnet/aspnetcore/blob/master/src/Security/Authentication/Certificate/src/CertificateAuthenticationHandler.cs

using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Configuration;

namespace Steeltoe.Security.Authentication.Mtls;

/// <summary>
/// This class is based on <see cref="CertificateAuthenticationHandler" />, but allows side-loading a root CA.
/// </summary>
internal sealed class MutualTlsAuthenticationHandler : AuthenticationHandler<MutualTlsAuthenticationOptions>
{
    private static readonly Oid ClientCertificateOid = new("1.3.6.1.5.5.7.3.2");
    private readonly IOptionsMonitor<CertificateOptions> _certificateOptionsMonitor;

    /// <summary>
    /// Gets the handler calls methods on the events which give the application control at certain points where processing is occurring. If it is not
    /// provided a default instance is supplied which does nothing when the methods are called.
    /// </summary>
    private new CertificateAuthenticationEvents Events => (CertificateAuthenticationEvents)base.Events;

#if NET6_0
    public MutualTlsAuthenticationHandler(IOptionsMonitor<MutualTlsAuthenticationOptions> options, IOptionsMonitor<CertificateOptions> certificateOptionsMonitor, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
#else
    public MutualTlsAuthenticationHandler(IOptionsMonitor<MutualTlsAuthenticationOptions> options,
        IOptionsMonitor<CertificateOptions> certificateOptionsMonitor, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
#endif
    {
        _certificateOptionsMonitor = certificateOptionsMonitor;
    }

    /// <summary>
    /// Creates a new instance of the events instance.
    /// </summary>
    /// <returns>
    /// A new instance of the events instance.
    /// </returns>
    protected override Task<object> CreateEventsAsync()
    {
        return Task.FromResult<object>(new CertificateAuthenticationEvents());
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // You only get client certificates over HTTPS
        if (!Context.Request.IsHttps)
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            X509Certificate2 clientCertificate = await Context.Connection.GetClientCertificateAsync();

            // This should never be the case, as cert authentication happens long before ASP.NET kicks in.
            if (clientCertificate == null)
            {
                Logger.NoCertificate();
                return AuthenticateResult.NoResult();
            }

            // If we have a self-signed cert, and they're not allowed, exit early and not bother with
            // any other validations.
            if (clientCertificate.IsSelfSigned() && !Options.AllowedCertificateTypes.HasFlag(CertificateTypes.SelfSigned))
            {
                Logger.LogWarning("Self signed certificate rejected, subject was {0}", clientCertificate.Subject);
                return AuthenticateResult.Fail("Options do not allow self signed certificates.");
            }

            // If we have a chained cert, and they're not allowed, exit early and not bother with
            // any other validations.
            if (!clientCertificate.IsSelfSigned() && !Options.AllowedCertificateTypes.HasFlag(CertificateTypes.Chained))
            {
                Logger.CertificateRejected("Chained", clientCertificate.Subject);
                return AuthenticateResult.Fail("Options do not allow chained certificates.");
            }

            X509ChainPolicy chainPolicy = BuildChainPolicy(clientCertificate);

            var chain = new X509Chain
            {
                ChainPolicy = chainPolicy
            };

            //// <variation>
            bool certificateIsValid = IsChainValid(chain, clientCertificate);
            //// </variation>

            if (!certificateIsValid)
            {
                var chainErrors = new List<string>();

                foreach (X509ChainStatus validationFailure in chain.ChainStatus)
                {
                    chainErrors.Add($"{validationFailure.Status} {validationFailure.StatusInformation}");
                }

                Logger.CertificateFailedValidation(clientCertificate.Subject, chainErrors);
                return AuthenticateResult.Fail("Client certificate failed validation.");
            }

            var certificateValidatedContext = new CertificateValidatedContext(Context, Scheme, Options)
            {
                ClientCertificate = clientCertificate,
                Principal = CreatePrincipal(clientCertificate)
            };

            await Events.CertificateValidated(certificateValidatedContext);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // https://github.com/dotnet/aspnetcore/issues/45594
            if (certificateValidatedContext.Result != null)
            {
                return certificateValidatedContext.Result;
            }

            // ReSharper disable once HeuristicUnreachableCode
            // https://github.com/dotnet/aspnetcore/issues/45594
            certificateValidatedContext.Success();
            return certificateValidatedContext.Result;
        }
        catch (Exception ex)
        {
            var authenticationFailedContext = new CertificateAuthenticationFailedContext(Context, Scheme, Options)
            {
                Exception = ex
            };

            await Events.AuthenticationFailed(authenticationFailedContext);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // https://github.com/dotnet/aspnetcore/issues/45594
            if (authenticationFailedContext.Result != null)
            {
                return authenticationFailedContext.Result;
            }

            throw;
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (Context.User.Claims.Any())
        {
            Context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        // Certificate authentication takes place at the connection level. We can't prompt once we're in
        // user code, so the best thing to do is Forbid, not Challenge.
        return HandleForbiddenAsync(properties);
    }

    /// <summary>
    /// Call chain.Build first, if !isValid then compares root with issuer chain known to this app.
    /// </summary>
    /// <param name="chain">
    /// Certificate chain to validate against.
    /// </param>
    /// <param name="certificate">
    /// Certificate to validate.
    /// </param>
    /// <returns>
    /// Indication of chain validity.
    /// </returns>
    private bool IsChainValid(X509Chain chain, X509Certificate2 certificate)
    {
        bool isValid = chain.Build(certificate);

        // allow root cert to be side loaded without installing into X509Store Root store
        if (!isValid && Array.TrueForAll(chain.ChainStatus,
            x => x.Status is X509ChainStatusFlags.UntrustedRoot or X509ChainStatusFlags.PartialChain or X509ChainStatusFlags.OfflineRevocation or
                X509ChainStatusFlags.RevocationStatusUnknown))
        {
            Logger.LogInformation("Certificate not valid by standard rules, trying custom validation");

            isValid = _certificateOptionsMonitor.Get("AppInstanceIdentity").IssuerChain
                .Intersect(ToGenericEnumerable(chain.ChainElements).Select(c => c.Certificate)).Any();
        }

        return isValid;
    }

    private static IEnumerable<X509ChainElement> ToGenericEnumerable(X509ChainElementCollection collection)
    {
        return collection;
    }

    private X509ChainPolicy BuildChainPolicy(X509Certificate2 certificate)
    {
        // Now build the chain validation options.
        X509RevocationFlag revocationFlag = Options.RevocationFlag;
        X509RevocationMode revocationMode = Options.RevocationMode;

        if (certificate.IsSelfSigned())
        {
            // Turn off chain validation, because we have a self signed certificate.
            revocationFlag = X509RevocationFlag.EntireChain;
            revocationMode = X509RevocationMode.NoCheck;
        }

        var chainPolicy = new X509ChainPolicy
        {
            RevocationFlag = revocationFlag,
            RevocationMode = revocationMode
        };

        //// <variation>
        foreach (X509Certificate2 chainCert in _certificateOptionsMonitor.Get("AppInstanceIdentity").IssuerChain)
        {
            chainPolicy.ExtraStore.Add(chainCert);
        }
        //// </variation>

        if (Options.ValidateCertificateUse)
        {
            chainPolicy.ApplicationPolicy.Add(ClientCertificateOid);
        }

        if (certificate.IsSelfSigned())
        {
            chainPolicy.VerificationFlags |= X509VerificationFlags.AllowUnknownCertificateAuthority;
            chainPolicy.VerificationFlags |= X509VerificationFlags.IgnoreEndRevocationUnknown;
            chainPolicy.ExtraStore.Add(certificate);
        }

        if (!Options.ValidateValidityPeriod)
        {
            chainPolicy.VerificationFlags |= X509VerificationFlags.IgnoreNotTimeValid;
        }

        return chainPolicy;
    }

    private ClaimsPrincipal CreatePrincipal(X509Certificate2 certificate)
    {
        var claims = new List<Claim>();

        string issuer = certificate.Issuer;
        claims.Add(new Claim("issuer", issuer, ClaimValueTypes.String, Options.ClaimsIssuer));

        string thumbprint = certificate.Thumbprint;
        claims.Add(new Claim(ClaimTypes.Thumbprint, thumbprint, ClaimValueTypes.Base64Binary, Options.ClaimsIssuer));

        string value = certificate.SubjectName.Name;

        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(ClaimTypes.X500DistinguishedName, value, ClaimValueTypes.String, Options.ClaimsIssuer));
        }

        value = certificate.SerialNumber;

        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(ClaimTypes.SerialNumber, value, ClaimValueTypes.String, Options.ClaimsIssuer));
        }

        MapClaimIfFound(certificate, X509NameType.DnsName, claims, ClaimTypes.Dns);
        MapClaimIfFound(certificate, X509NameType.SimpleName, claims, ClaimTypes.Name);
        MapClaimIfFound(certificate, X509NameType.EmailName, claims, ClaimTypes.Email);
        MapClaimIfFound(certificate, X509NameType.UpnName, claims, ClaimTypes.Upn);
        MapClaimIfFound(certificate, X509NameType.UrlName, claims, ClaimTypes.Uri);

        var identity = new ClaimsIdentity(claims, CertificateAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private void MapClaimIfFound(X509Certificate2 certificate, X509NameType claimSource, List<Claim> claims, string claimDestination)
    {
        string value = certificate.GetNameInfo(claimSource, false);

        if (!string.IsNullOrWhiteSpace(value))
        {
            claims.Add(new Claim(claimDestination, value, ClaimValueTypes.String, Options.ClaimsIssuer));
        }
    }
}
