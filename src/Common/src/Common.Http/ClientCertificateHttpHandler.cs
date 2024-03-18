// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;

namespace Steeltoe.Common.Http;

public sealed class ClientCertificateHttpHandler : HttpClientHandler
{
    private readonly SemaphoreSlim _lock = new(1);
    private readonly IOptionsMonitor<CertificateOptions> _certificateOptions;
    private readonly string _name;
    private CertificateOptions _lastValue;

    public ClientCertificateHttpHandler(IOptionsMonitor<CertificateOptions> certOptions)
        : this(certOptions, string.Empty)
    {
    }

    public ClientCertificateHttpHandler(IOptionsMonitor<CertificateOptions> certOptions, string name)
    {
        _certificateOptions = certOptions;
        _name = name;
        _certificateOptions.OnChange(RotateCert);
        RotateCert(_certificateOptions.CurrentValue);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    // ReSharper disable once RedundantAssignment
    private void RotateCert(CertificateOptions newCert)
    {
        newCert = _certificateOptions.Get(_name);
        if (newCert.Certificate == null || newCert.Certificate.Equals(_lastValue?.Certificate))
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
        }

        personalCertStore.Certificates.Add(newCert.Certificate);

        personalCertStore.Close();
        authorityCertStore.Close();
        _lock.Wait();

        try
        {
            _lastValue = _certificateOptions.CurrentValue;
            ClientCertificates.Add(_lastValue.Certificate);
        }
        finally
        {
            _lock.Release();
        }
    }
}
