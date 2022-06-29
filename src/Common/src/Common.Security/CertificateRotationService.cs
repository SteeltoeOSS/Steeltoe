// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Security;

public class CertificateRotationService : IDisposable, ICertificateRotationService
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

    protected virtual void Dispose(bool disposing)
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
        if (_lastValue != null)
        {
            personalCertStore.Certificates.Remove(_lastValue.Certificate);
            foreach (var cert in _lastValue.IssuerChain)
            {
                personalCertStore.Certificates.Remove(cert);
            }
        }

        personalCertStore.Certificates.Add(newCert.Certificate);
        foreach (var cert in newCert.IssuerChain)
        {
            personalCertStore.Certificates.Add(cert);
        }

        personalCertStore.Close();
        authorityCertStore.Close();
    }
}
