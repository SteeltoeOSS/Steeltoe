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
