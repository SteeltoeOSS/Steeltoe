// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Security;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test;

public sealed class ClientCertificatesFixture : IDisposable
{
    public LocalCertificateWriter CertificateWriter { get; } = new();

    public Guid ServerOrgId { get; } = new("a8fef16f-94c0-49e3-aa0b-ced7c3da6229");
    public Guid ServerSpaceId { get; } = new("122b942a-d7b9-4839-b26e-836654b9785f");

    public ClientCertificatesFixture()
    {
        CertificateWriter.CertificateFilenamePrefix = "OrgAndSpaceMatch";
        CertificateWriter.Write(ServerOrgId, ServerSpaceId);

        CertificateWriter.CertificateFilenamePrefix = "SpaceMatch";
        CertificateWriter.Write(Guid.NewGuid(), ServerSpaceId);

        CertificateWriter.CertificateFilenamePrefix = "OrgMatch";
        CertificateWriter.Write(ServerOrgId, Guid.NewGuid());
    }

    public void Dispose()
    {
    }
}
