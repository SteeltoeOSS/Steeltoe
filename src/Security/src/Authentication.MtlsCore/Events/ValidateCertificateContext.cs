// Copyright (c) Barry Dorrans. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Steeltoe.Common.Security;

namespace Steeltoe.Security.Authentication.MtlsCore.Events
{
    public class ValidateCertificateContext : ResultContext<CertificateAuthenticationOptions>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ValidateCertificateContext"/>.
        /// </summary>
        /// <param name="context">The HttpContext the validate context applies too.</param>
        /// <param name="scheme">The scheme used when the Basic Authentication handler was registered.</param>
        /// <param name="options">The <see cref="BasicAuthenticationOptions"/> for the instance of
        /// <see cref="BasicAuthenticationMiddleware"/> creating this instance.</param>
        /// <param name="ticket">Contains the intial values for the identit.</param>
        public ValidateCertificateContext(
            HttpContext context,
            AuthenticationScheme scheme,
            CertificateAuthenticationOptions options)
            : base(context, scheme, options)
        {
        }

        /// <summary>
        /// The certificate to validate.
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; }
        
        public List<Claim> GetDefaultClaims()
        {
            var certificate = ClientCertificate;
            var claims = new List<Claim>();

            var issuer = certificate.Issuer;
            claims.Add(new Claim("issuer", issuer, ClaimValueTypes.String, Options.ClaimsIssuer));

            var thumbprint = certificate.Thumbprint;
            claims.Add(new Claim(ClaimTypes.Thumbprint, thumbprint, ClaimValueTypes.Base64Binary, Options.ClaimsIssuer));

            var value = certificate.SubjectName.Name;
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.X500DistinguishedName, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.SerialNumber;
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.SerialNumber, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.DnsName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Dns, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.SimpleName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Name, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.EmailName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Email, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.UpnName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Upn, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.UrlName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Uri, value, ClaimValueTypes.String, Options.ClaimsIssuer));
            }

            return claims;
            
        }

    }
}
