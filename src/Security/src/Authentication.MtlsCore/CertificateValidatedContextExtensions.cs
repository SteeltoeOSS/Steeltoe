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

using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Authentication.Certificate
{
    public static class CertificateValidatedContextExtensions
    {
        public static List<Claim> GetDefaultClaims(this CertificateValidatedContext context)
        {
            var certificate = context.ClientCertificate;
            var claims = new List<Claim>();

            var issuer = certificate.Issuer;
            claims.Add(new Claim("issuer", issuer, ClaimValueTypes.String, context.Options.ClaimsIssuer));

            var thumbprint = certificate.Thumbprint;
            claims.Add(new Claim(ClaimTypes.Thumbprint, thumbprint, ClaimValueTypes.Base64Binary, context.Options.ClaimsIssuer));

            var value = certificate.SubjectName.Name;
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.X500DistinguishedName, value, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            value = certificate.SerialNumber;
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.SerialNumber, value, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.DnsName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Dns, value, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.SimpleName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Name, value, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.EmailName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Email, value, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.UpnName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Upn, value, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            value = certificate.GetNameInfo(X509NameType.UrlName, false);
            if (!string.IsNullOrWhiteSpace(value))
            {
                claims.Add(new Claim(ClaimTypes.Uri, value, ClaimValueTypes.String, context.Options.ClaimsIssuer));
            }

            return claims;
        }
    }
}
