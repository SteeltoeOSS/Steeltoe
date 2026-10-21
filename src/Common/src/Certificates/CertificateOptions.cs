// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Certificates;

/// <summary>
/// Options for use with platform-provided certificates.
/// </summary>
public sealed class CertificateOptions
{
    internal const string ConfigurationKeyPrefix = "Certificates";

    // We don't dispose certificates that are loaded from files, because they can still be in use after the options change.
    // On Windows, loading a certificate with a private key writes key material to %APPDATA%\Microsoft\Crypto\Keys.
    // Such a key file is deleted when the certificate is disposed or finalized, but not when the process exits first
    // (finalizers do not run at exit). Key files can therefore be left behind, which can be avoided by disposing certificates
    // once done with them.
    // On Windows, a certificate that is used as a client certificate by HttpClient keeps its key file after it is disposed,
    // until it is finalized. This is a limitation in the .NET runtime, see https://github.com/dotnet/runtime/issues/101090.

    public X509Certificate2? Certificate { get; set; }
    public IList<X509Certificate2> IssuerChain { get; } = [];

    internal CertificateOptions Clone()
    {
        var clone = new CertificateOptions
        {
            Certificate = Certificate
        };

        foreach (X509Certificate2 issuer in IssuerChain)
        {
            clone.IssuerChain.Add(issuer);
        }

        return clone;
    }
}
