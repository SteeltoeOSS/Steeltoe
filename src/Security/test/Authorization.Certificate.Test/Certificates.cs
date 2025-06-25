// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Steeltoe.Security.Authorization.Certificate.Test;

internal static class Certificates
{
    private const string ServerEku = "1.3.6.1.5.5.7.3.1";
    private const string ClientEku = "1.3.6.1.5.5.7.3.2";

    private static readonly X509KeyUsageExtension SDigitalSignatureOnlyUsage = new(X509KeyUsageFlags.DigitalSignature, true);
    internal static Guid ServerOrgId { get; } = new("a8fef16f-94c0-49e3-aa0b-ced7c3da6229");
    internal static Guid ServerSpaceId { get; } = new("122b942a-d7b9-4839-b26e-836654b9785f");

    public static X509Certificate2 OrgMatch { get; private set; }

    public static X509Certificate2 SpaceMatch { get; private set; }

    public static X509Certificate2 FromDiego { get; } = X509Certificate2.CreateFromPemFile("instance.crt", "instance.key");

    static Certificates()
    {
        DateTimeOffset now = TimeProvider.System.GetUtcNow();

        OrgMatch = MakeCert(SubjectName(ServerOrgId.ToString(), Guid.NewGuid().ToString()), now);
        SpaceMatch = MakeCert(SubjectName(Guid.NewGuid().ToString(), ServerSpaceId.ToString()), now);
    }

    private static string SubjectName(string orgId, string spaceId)
    {
        return $"CN={Guid.NewGuid()}, OU=app:{Guid.NewGuid()} + OU=space:{spaceId} + OU=organization:{orgId}";
    }

    private static X509Certificate2 MakeCert(string subjectName, DateTimeOffset now)
    {
        return MakeCert(subjectName, now, now.AddYears(5));
    }

    private static X509Certificate2 MakeCert(string subjectName, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var key = RSA.Create(2048);

        var request = new CertificateRequest(subjectName, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(SDigitalSignatureOnlyUsage);

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([
            new Oid(ClientEku),
            new Oid(ServerEku)
        ], false));

        return request.CreateSelfSigned(notBefore, notAfter);
    }
}
