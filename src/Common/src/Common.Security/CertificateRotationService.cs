using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Common.Security
{
    public class CertificateRotationService : IDisposable, ICertificateRotationService
    {
        private readonly IOptionsMonitor<CertificateOptions> _optionsMonitor;
        private readonly IDisposable _subscription;
        private CertificateOptions _lastValue;
        private bool _isStarted = false;

        public CertificateRotationService(IOptionsMonitor<CertificateOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
            _subscription = _optionsMonitor.OnChange(RotateCert);
        }

        public void Start()
        {
            if (_isStarted)
            {
                return;
            }

            RotateCert(_optionsMonitor.CurrentValue);
            _lastValue = _optionsMonitor.CurrentValue;
        }

        public void Dispose()
        {
            _subscription.Dispose();
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
}