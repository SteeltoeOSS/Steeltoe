// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;

namespace Steeltoe.Security.Authentication.Mtls;

public class MutualTlsAuthenticationOptions : CertificateAuthenticationOptions
{
    /// <summary>
    /// Gets or sets partial or full certificate chain for validation.
    /// </summary>
    public ICollection<X509Certificate2> IssuerChain { get; set; } = new List<X509Certificate2>();
}
