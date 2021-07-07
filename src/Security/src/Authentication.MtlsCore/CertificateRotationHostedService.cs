// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Security.Authentication.Mtls
{
    public class CertificateRotationHostedService : IHostedService
    {
        private readonly ICertificateRotationService _certificateRotationService;

        public CertificateRotationHostedService(ICertificateRotationService certificateRotationService)
        {
            _certificateRotationService = certificateRotationService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _certificateRotationService.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
