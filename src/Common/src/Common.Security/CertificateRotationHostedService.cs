// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;

namespace Steeltoe.Common.Security;

internal sealed class CertificateRotationHostedService(CertificateRotationService certificateRotationService) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        certificateRotationService.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
