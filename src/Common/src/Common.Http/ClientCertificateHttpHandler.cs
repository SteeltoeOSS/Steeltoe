// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Common.Http;

public class ClientCertificateHttpHandler : HttpClientHandler
{
    private readonly SemaphoreSlim _lock = new (1);
    private readonly IOptionsMonitor<CertificateOptions> _certificateOptions;
    private CertificateOptions _lastValue;

    public ClientCertificateHttpHandler(IOptionsMonitor<CertificateOptions> certOptions)
    {
        _certificateOptions = certOptions;
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