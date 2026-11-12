// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;

namespace Steeltoe.Common.Certificates.Test;

/// <summary>
/// Collects every certificate (and issuer) that tests obtain from certificate options, including intermediate ones that tests never see, and disposes
/// them, so that no private key material is left on disk.
/// </summary>
internal sealed class CertificateCollector : IDisposable
{
#pragma warning disable IDE0028 // Simplify collection initialization
    // Justification: workaround for bug https://youtrack.jetbrains.com/issue/RSRP-505438/Collection-expression-with-with-expression-requires-language-version-preview
    private readonly HashSet<X509Certificate2> _certificatesToDispose = new(ReferenceEqualityComparer.Instance);
#pragma warning restore IDE0028 // Simplify collection initialization
    private IOptionsMonitor<CertificateOptions>? _optionsMonitor;
    private string? _certificateName;
    private IDisposable? _changeListener;

    public void Attach(IOptionsMonitor<CertificateOptions> optionsMonitor, string certificateName)
    {
        _optionsMonitor = optionsMonitor;
        _certificateName = certificateName;

        GetCurrent();

        _changeListener = optionsMonitor.OnChange((options, _) => Collect(options));
    }

    /// <summary>
    /// Gets the current certificate from the options monitor, which may create a new one when the options were invalidated in the meantime.
    /// </summary>
    public X509Certificate2? GetCurrent()
    {
        if (_optionsMonitor != null)
        {
            CertificateOptions options = _optionsMonitor.Get(_certificateName);
            Collect(options);
            return options.Certificate;
        }

        return null;
    }

    /// <summary>
    /// Takes ownership of the certificate and issuer chain in the specified options.
    /// </summary>
    public void Collect(CertificateOptions options)
    {
        Add(options.Certificate);

        foreach (X509Certificate2 issuer in options.IssuerChain)
        {
            Add(issuer);
        }
    }

    private void Add(X509Certificate2? certificate)
    {
        if (certificate != null)
        {
            lock (_certificatesToDispose)
            {
                _certificatesToDispose.Add(certificate);
            }
        }
    }

    public void Dispose()
    {
        _changeListener?.Dispose();

        lock (_certificatesToDispose)
        {
            foreach (X509Certificate2 certificate in _certificatesToDispose)
            {
                certificate.Dispose();
            }

            _certificatesToDispose.Clear();
        }
    }
}
