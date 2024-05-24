// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Security;

public sealed class CertificateRotationService : IDisposable
{
    private readonly IOptionsMonitor<CertificateOptions> _optionsMonitor;
    private readonly IDisposable _subscription;
    private CertificateOptions _lastValue;

    public CertificateRotationService(IOptionsMonitor<CertificateOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        _subscription = _optionsMonitor.OnChange(RotateCert);
    }

    public void Start()
    {
        RotateCert(_optionsMonitor.CurrentValue);
        _lastValue = _optionsMonitor.CurrentValue;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscription.Dispose();
        }
    }

    private void RotateCert(CertificateOptions newCert)
    {
        if (newCert.Certificate == null)
        {
            return;
        }

        var personalCertStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        var authorityCertStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
        personalCertStore.Open(OpenFlags.ReadWrite);
        authorityCertStore.Open(OpenFlags.ReadWrite);

        if (_lastValue != null && personalCertStore.Certificates.Contains(_lastValue.Certificate))
        {
            personalCertStore.Certificates.Remove(_lastValue.Certificate);

            foreach (X509Certificate2 cert in _lastValue.IssuerChain)
            {
                personalCertStore.Certificates.Remove(cert);
            }
        }

        personalCertStore.Certificates.Add(newCert.Certificate);

        foreach (X509Certificate2 cert in newCert.IssuerChain)
        {
            personalCertStore.Certificates.Add(cert);
        }

        personalCertStore.Close();
        authorityCertStore.Close();
    }
}
