// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Http
{
    public class ClientCertificateHttpHandlerProvider : IHttpClientHandlerProvider
    {
        private readonly IOptionsMonitor<CertificateOptions> certificateOptions;
        private CertificateOptions _lastValue;

        public ClientCertificateHttpHandlerProvider(IOptionsMonitor<CertificateOptions> certOptions)
        {
            certificateOptions = certOptions;
            certificateOptions.OnChange(RotateCert);
            RotateCert(certificateOptions.CurrentValue);
        }

        public HttpClientHandler GetHttpClientHandler()
        {
            var handler = new HttpClientHandler();
            if (certificateOptions?.CurrentValue?.Certificate != null)
            {
                handler.ClientCertificates.Add(_lastValue.Certificate);
            }

            return handler;
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
            _lastValue = certificateOptions.CurrentValue;
        }
    }
}
